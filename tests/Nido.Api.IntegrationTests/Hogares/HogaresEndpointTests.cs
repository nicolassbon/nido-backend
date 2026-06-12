using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Application.Auth.Interfaces;
using Nido.Api.IntegrationTests.Auth;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Api.IntegrationTests.Hogares;

public sealed class HogaresEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public HogaresEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMiembros_DevuelveAlergiasDelUsuario()
    {
        var registered = await RegisterAndAuthenticateAsync(_client, "hogar-miembros");
        var restriccionId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.RestriccionesCatalogo.Add(new RestriccionesCatalogo
            {
                Id = restriccionId,
                Nombre = "Gluten",
                Tipo = "alergia"
            });
            db.RestriccionesUsuarios.Add(new RestriccionesUsuario
            {
                UsuarioId = registered.UsuarioId,
                RestriccionId = restriccionId
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/hogares/miembros");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var miembros = await response.Content.ReadFromJsonAsync<List<MiembroBody>>();
        var miembro = Assert.Single(miembros!);
        Assert.Equal(registered.UsuarioId, miembro.UsuarioId);
        Assert.Contains("Gluten", miembro.Alergias);
    }

    [Fact]
    public async Task GetInvitacionPreview_CuandoInvitacionPendiente_NoRequiereAuthYDevuelveDatos()
    {
        var owner = await RegisterAndAuthenticateAsync(_client, "hogar-preview-owner", "Preview Owner");
        var token = $"preview-{Guid.NewGuid():N}";
        var expiresAt = DateTime.UtcNow.AddDays(3);

        await SeedInvitationAsync(owner.HogarId, owner.UsuarioId, token, "guest-preview@test.com", "pendiente", expiresAt);

        var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.GetAsync($"/api/hogares/invitaciones/{token}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<InvitacionPreviewBody>();
        Assert.NotNull(body);
        Assert.Equal("Hogar de Preview Owner", body!.HogarNombre);
        Assert.Equal("guest-preview@test.com", body.EmailInvitado);
        Assert.NotNull(body.ExpiraEn);
        Assert.InRange(body.ExpiraEn!.Value, expiresAt.AddSeconds(-1), expiresAt.AddSeconds(1));
    }

    [Fact]
    public async Task GetInvitacionPreview_CuandoInvitacionExpirada_DevuelveGone()
    {
        var owner = await RegisterAndAuthenticateAsync(_client, "hogar-preview-expired", "Expired Owner");
        var token = $"expired-{Guid.NewGuid():N}";

        await SeedInvitationAsync(owner.HogarId, owner.UsuarioId, token, "guest-expired@test.com", "pendiente", DateTime.UtcNow.AddMinutes(-5));

        var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.GetAsync($"/api/hogares/invitaciones/{token}");

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(410, problem!.Status);
        Assert.Equal("INVITATION_EXPIRED", problem.Title);
    }

    [Fact]
    public async Task RemoveMiembro_CuandoOwnerEliminaAConviviente_RemueveMembresiaYLeCreaNuevoHogar()
    {
        var owner = await RegisterAndAuthenticateAsync(_client, "hogar-remove-owner", "Owner Remove");
        var memberClient = _factory.CreateClient();
        var member = await RegisterAndAuthenticateAsync(memberClient, "hogar-remove-member", "Guest Remove");

        await MoveUserIntoHouseholdAsync(member, owner, role: "conviviente");

        var response = await _client.DeleteAsync($"/api/hogares/miembros/{member.UsuarioId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        Assert.False(await db.MiembrosHogars.AnyAsync(x => x.HogarId == owner.HogarId && x.UsuarioId == member.UsuarioId));

        var newMembership = await db.MiembrosHogars.SingleAsync(x => x.UsuarioId == member.UsuarioId);
        Assert.Equal("owner", newMembership.Rol);
        Assert.NotEqual(owner.HogarId, newMembership.HogarId);

        var newHogar = await db.Hogares.SingleAsync(x => x.Id == newMembership.HogarId);
        Assert.Equal("Hogar de Guest Remove", newHogar.Nombre);
    }

    [Fact]
    public async Task RemoveMiembro_CuandoCallerNoEsOwner_DevuelveForbidden()
    {
        var owner = await RegisterAndAuthenticateAsync(_client, "hogar-remove-auth-owner", "Owner Auth");
        var memberClient = _factory.CreateClient();
        var member = await RegisterAndAuthenticateAsync(memberClient, "hogar-remove-auth-member", "Member Auth");

        var memberToken = await MoveUserIntoHouseholdAsync(member, owner, role: "conviviente");
        memberClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var response = await memberClient.DeleteAsync($"/api/hogares/miembros/{owner.UsuarioId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(403, problem!.Status);
        Assert.Equal("NOT_HOUSEHOLD_OWNER", problem.Title);
    }

    [Fact]
    public async Task RemoveMiembro_CuandoTargetPerteneceAOtroHogar_DevuelveNotFound()
    {
        var owner = await RegisterAndAuthenticateAsync(_client, "hogar-remove-isolation-owner", "Isolation Owner");
        var outsiderClient = _factory.CreateClient();
        var outsider = await RegisterAndAuthenticateAsync(outsiderClient, "hogar-remove-isolation-outsider", "Isolation Outsider");

        var response = await _client.DeleteAsync($"/api/hogares/miembros/{outsider.UsuarioId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem!.Status);
        Assert.Equal("NOT_HOUSEHOLD_MEMBER", problem.Title);
    }

    private async Task<AuthenticatedUser> RegisterAndAuthenticateAsync(HttpClient client, string prefix, string name = "Test User")
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@test.com";
        using var effectiveReq = RegisterMultipartRequest.Create(name, email, "Password123!", "U");
        var res = await client.PostAsync("/api/auth/register", effectiveReq);
        var body = await res.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(body);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return new AuthenticatedUser(body.UsuarioId, body.HogarId, body.AccessToken, email, name);
    }

    private async Task SeedInvitationAsync(Guid hogarId, Guid invitadoPor, string token, string emailInvitado, string estado, DateTime? expiraEn)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        db.InvitacionesHogars.Add(new InvitacionesHogar
        {
            Id = Guid.NewGuid(),
            HogarId = hogarId,
            InvitadoPor = invitadoPor,
            Token = token,
            EmailInvitado = emailInvitado,
            Estado = estado,
            ExpiraEn = expiraEn,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private async Task<string> MoveUserIntoHouseholdAsync(AuthenticatedUser user, AuthenticatedUser owner, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var membership = await db.MiembrosHogars.SingleAsync(x => x.UsuarioId == user.UsuarioId && x.HogarId == user.HogarId);
        membership.HogarId = owner.HogarId;
        membership.Rol = role;
        await db.SaveChangesAsync();

        return tokenService.CreateToken(user.UsuarioId, owner.HogarId, user.Email, user.Nombre);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);
    private sealed record AuthenticatedUser(Guid UsuarioId, Guid HogarId, string AccessToken, string Email, string Nombre);
    private sealed record ProblemDetailsBody(int Status, string? Title, string? Detail);
    private sealed record InvitacionPreviewBody(string HogarNombre, string? EmailInvitado, DateTime? ExpiraEn);
    private sealed record MiembroBody(Guid UsuarioId, string Nombre, List<string> Alergias);
}
