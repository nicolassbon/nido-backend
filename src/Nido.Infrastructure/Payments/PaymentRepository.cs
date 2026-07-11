using Microsoft.EntityFrameworkCore;
using Nido.Application.Payments;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Payments;

public sealed class PaymentRepository : IPaymentRepository, IDevelopmentEntitlementRepository
{
    private readonly NidoDbContext _context;

    public PaymentRepository(NidoDbContext context)
    {
        _context = context;
    }

    public async Task<HouseholdEntitlement> GetSubscriptionAsync(Guid hogarId, CancellationToken ct)
    {
        var hogar = await _context.Hogares
            .AsNoTracking()
            .Where(h => h.Id == hogarId)
            .Select(h => new { h.Plan, h.SubscriptionStatus, h.TrialEndsAt, h.SuscripcionVenceEl })
            .SingleOrDefaultAsync(ct);

        if (hogar is null)
        {
            throw new InvalidOperationException($"Household (Hogar) with ID '{hogarId}' was not found.");
        }

        return new HouseholdEntitlement(
            ParsePlan(hogar.Plan),
            ParseSubscriptionStatus(hogar.SubscriptionStatus),
            hogar.TrialEndsAt.HasValue ? hogar.TrialEndsAt.Value.ToUniversalTime() : null,
            hogar.SuscripcionVenceEl.HasValue ? hogar.SuscripcionVenceEl.Value.ToUniversalTime() : null);
    }

    public async Task<ProcessWebhookOutcome> ProcessWebhookEventAsync(PaymentWebhookEventRecord webhookEvent, PaymentPlanUpdate planUpdate, CancellationToken ct)
    {
        var existing = await _context.PaymentWebhookEvents
            .AsNoTracking()
            .AnyAsync(e => e.Provider == webhookEvent.Provider && e.ProviderEventId == webhookEvent.ProviderEventId, ct);

        if (existing)
        {
            return ProcessWebhookOutcome.Duplicate;
        }

        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({planUpdate.HogarId.ToString()}))", ct);
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM hogares WHERE id = {planUpdate.HogarId} FOR UPDATE", ct);

            var newEvent = new PaymentWebhookEvent
            {
                Id = Guid.NewGuid(),
                Provider = webhookEvent.Provider,
                ProviderEventId = webhookEvent.ProviderEventId,
                ProviderPaymentId = webhookEvent.ProviderPaymentId,
                ProviderSubscriptionId = webhookEvent.ProviderSubscriptionId,
                EventType = webhookEvent.EventType,
                Payload = webhookEvent.Payload,
                HogarId = webhookEvent.HogarId,
                ReceivedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            _context.PaymentWebhookEvents.Add(newEvent);

            var hogarExists = await _context.Hogares.AnyAsync(h => h.Id == planUpdate.HogarId, ct);
            if (!hogarExists)
            {
                throw new InvalidOperationException($"Household (Hogar) with ID '{planUpdate.HogarId}' was not found.");
            }

            if (planUpdate.SubscriptionStatus == SubscriptionStatus.Active || planUpdate.SubscriptionStatus == SubscriptionStatus.Cancelled)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE hogares
                    SET plan = {planUpdate.Plan.ToResponseString()},
                        subscription_status = {planUpdate.SubscriptionStatus.ToResponseString(planUpdate.Plan)},
                        mercado_pago_subscription_id = {planUpdate.ProviderSubscriptionId},
                        mercado_pago_payment_id = {planUpdate.ProviderPaymentId},
                        plan_updated_at = {DateTime.UtcNow},
                        provider_transition_at = {planUpdate.ProviderTransitionAt},
                        suscripcion_vence_el = {planUpdate.SubscriptionEndsAt}
                    WHERE id = {planUpdate.HogarId}
                      AND (provider_transition_at IS NULL OR provider_transition_at <= {planUpdate.ProviderTransitionAt})
                      AND (
                          {planUpdate.SubscriptionStatus.ToResponseString(planUpdate.Plan)} <> 'cancelled'
                          OR mercado_pago_payment_id IS NULL
                          OR mercado_pago_payment_id = {planUpdate.ProviderPaymentId})
                    """, ct);
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return ProcessWebhookOutcome.Processed;
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
        {
            await transaction.RollbackAsync(ct);
            return ProcessWebhookOutcome.Duplicate;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<HouseholdEntitlement> SetAsync(
        Guid hogarId,
        HouseholdPlan plan,
        DateTime nowUtc,
        DateTime? subscriptionEndsAt,
        CancellationToken ct)
    {
        var affected = await _context.Hogares
            .Where(h => h.Id == hogarId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(h => h.Plan, plan.ToResponseString())
                .SetProperty(h => h.SubscriptionStatus, plan == HouseholdPlan.Premium ? "active" : "none")
                .SetProperty(h => h.TrialEndsAt, (DateTime?)null)
                .SetProperty(h => h.GracePeriodEndsAt, (DateTime?)null)
                .SetProperty(h => h.MercadoPagoCustomerId, (string?)null)
                .SetProperty(h => h.MercadoPagoSubscriptionId, (string?)null)
                .SetProperty(h => h.MercadoPagoPaymentId, (string?)null)
                .SetProperty(h => h.ProviderTransitionAt, (DateTime?)null)
                .SetProperty(h => h.PlanUpdatedAt, nowUtc)
                .SetProperty(h => h.SuscripcionVenceEl, subscriptionEndsAt), ct);

        if (affected == 0)
        {
            throw new InvalidOperationException($"Household (Hogar) with ID '{hogarId}' was not found.");
        }

        return new HouseholdEntitlement(
            plan,
            plan == HouseholdPlan.Premium ? SubscriptionStatus.Active : SubscriptionStatus.None,
            null,
            subscriptionEndsAt);
    }

    private static HouseholdPlan ParsePlan(string value) => value.ToLowerInvariant() switch
    {
        "premium" => HouseholdPlan.Premium,
        _ => HouseholdPlan.Free
    };

    private static SubscriptionStatus ParseSubscriptionStatus(string value) => value.ToLowerInvariant() switch
    {
        "pending" => SubscriptionStatus.Pending,
        "active" => SubscriptionStatus.Active,
        "past_due" => SubscriptionStatus.PastDue,
        "cancelled" => SubscriptionStatus.Cancelled,
        _ => SubscriptionStatus.None
    };

}
