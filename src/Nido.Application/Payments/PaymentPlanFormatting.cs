namespace Nido.Application.Payments;

public static class PaymentPlanFormatting
{
    public static string ToResponseString(this HouseholdPlan plan) => plan switch
    {
        HouseholdPlan.Premium => "premium",
        _ => "free"
    };

    public static string ToJwtClaimString(this HouseholdPlan plan) => plan switch
    {
        HouseholdPlan.Premium => "Hogar",
        _ => "Básico"
    };

    public static string ToResponseString(this SubscriptionStatus status, HouseholdPlan plan) => status switch
    {
        SubscriptionStatus.Pending => "pending",
        SubscriptionStatus.Active => "active",
        SubscriptionStatus.PastDue => "past_due",
        SubscriptionStatus.Cancelled => "cancelled",
        _ => plan == HouseholdPlan.Premium ? "none" : "free"
    };

    public static string ToJwtClaimString(this SubscriptionStatus status) => status switch
    {
        SubscriptionStatus.Pending => "pending",
        SubscriptionStatus.Active => "active",
        SubscriptionStatus.PastDue => "past_due",
        SubscriptionStatus.Cancelled => "cancelled",
        _ => "none"
    };
}
