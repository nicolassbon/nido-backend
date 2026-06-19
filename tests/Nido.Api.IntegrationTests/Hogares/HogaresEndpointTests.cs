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
    public async Task GetMiembros_NormalizaRolConvivienteComoIntegrante()
    {
        var registered = await RegisterAndAuthenticateAsync(_client, "hogar-integrante");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

            var invitedUser = new Usuario
            {
                Id = Guid.NewGuid(),
                Nombre = "Invitado",
                Email = $"integrante-{Guid.NewGuid():N}@test.com",
                Sexo = "U",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Usuarios.Add(invitedUser);
            db.MiembrosHogars.Add(new MiembrosHogar
            {
                Id = Guid.NewGuid(),
                UsuarioId = invitedUser.Id,
                HogarId = registered.HogarId,
                Rol = "conviviente",
                Puntos = 0
            });

            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/hogares/miembros");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var miembros = await response.Content.ReadFromJsonAsync<List<MiembroBody>>();
        Assert.NotNull(miembros);

        var integrante = miembros!.Single(x => x.UsuarioId != registered.UsuarioId);
        Assert.Equal("integrante", integrante.Rol);
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

    [Fact]
    public async Task InvitarConvivente_CuandoUsuarioNoEsOwner_DevuelveForbidden()
    {
        var owner = await RegisterAndAuthenticateAsync(_client, "invitar-owner", "Owner Invite");
        var memberClient = _factory.CreateClient();
        var member = await RegisterAndAuthenticateAsync(memberClient, "invitar-member", "Member Invite");
        var memberToken = await MoveUserIntoHouseholdAsync(member, owner, role: "conviviente");
        memberClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var response = await memberClient.PostAsJsonAsync("/api/hogares/invitar", new { EmailInvitado = "guest@test.com" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(403, problem!.Status);
        Assert.Equal("NOT_HOUSEHOLD_OWNER", problem.Title);
    }

    [Fact]
    public async Task InvitarConvivente_CuandoEmailVacio_DevuelveBadRequest()
    {
        var owner = await RegisterAndAuthenticateAsync(_client, "invitar-empty", "Owner Empty");

        var response = await _client.PostAsJsonAsync("/api/hogares/invitar", new { EmailInvitado = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
        Assert.Equal("MISSING_INVITATION_TOKEN", problem.Title);
    }

    [Fact]
    public async Task InvitarConvivente_CuandoHogarLleno_DevuelveConflict()
    {
        var owner = await RegisterAndAuthenticateAsync(_client, "invitar-full", "Owner Full");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            for (int i = 0; i < 5; i++)
            {
                var uid = Guid.NewGuid();
                db.Usuarios.Add(new Usuario
                {
                    Id = uid,
                    Nombre = $"Miembro {i}",
                    Email = $"fill{i}-{Guid.NewGuid():N}@test.com",
                    PasswordHash = "x",
                    Sexo = "U",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                db.MiembrosHogars.Add(new MiembrosHogar
                {
                    Id = Guid.NewGuid(),
                    UsuarioId = uid,
                    HogarId = owner.HogarId,
                    Rol = "conviviente"
                });
            }
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync("/api/hogares/invitar", new { EmailInvitado = "guest@test.com" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
        Assert.Equal("MAX_MEMBERS_EXCEEDED", problem.Title);
    }

    [Fact]
    public async Task AceptarInvitacion_CuandoTokenValido_UsuarioCambiaDeHogarYRecibeNuevoTokenQueFunciona()
    {
        var owner = await RegisterAndAuthenticateAsync(_client, "aceptar-owner", "Owner Accept");
        var inviteeClient = _factory.CreateClient();
        var invitee = await RegisterAndAuthenticateAsync(inviteeClient, "aceptar-invitee", "Invitee Accept");

        var token = $"aceptar-{Guid.NewGuid():N}";
        await SeedInvitationAsync(owner.HogarId, owner.UsuarioId, token, invitee.Email, "pendiente", DateTime.UtcNow.AddDays(7));

        var response = await inviteeClient.PostAsJsonAsync("/api/hogares/aceptar-invitacion", new { Token = token });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AceptarInvitacionBody>();
        Assert.NotNull(body);
        Assert.Equal(owner.HogarId, body!.HogarId);
        Assert.Equal("Hogar de Owner Accept", body.HogarNombre);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));

        var newClient = _factory.CreateClient();
        newClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        var miembrosResponse = await newClient.GetAsync("/api/hogares/miembros");
        Assert.Equal(HttpStatusCode.OK, miembrosResponse.StatusCode);
        var miembros = await miembrosResponse.Content.ReadFromJsonAsync<List<MiembroBody>>();
        Assert.NotNull(miembros);
        Assert.Contains(miembros!, m => m.UsuarioId == owner.UsuarioId);
        Assert.Contains(miembros!, m => m.UsuarioId == invitee.UsuarioId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var membership = await db.MiembrosHogars.SingleAsync(x => x.UsuarioId == invitee.UsuarioId && x.HogarId == owner.HogarId);
        Assert.Equal("integrante", membership.Rol);
    }

    [Fact]
    public async Task AceptarInvitacion_CuandoTokenExpirado_DevuelveGone()
    {
        var owner = await RegisterAndAuthenticateAsync(_client, "aceptar-exp-owner", "Owner Expired");
        var inviteeClient = _factory.CreateClient();
        var invitee = await RegisterAndAuthenticateAsync(inviteeClient, "aceptar-exp-invitee", "Invitee Expired");

        var token = $"exp-aceptar-{Guid.NewGuid():N}";
        await SeedInvitationAsync(owner.HogarId, owner.UsuarioId, token, invitee.Email, "pendiente", DateTime.UtcNow.AddMinutes(-5));

        var response = await inviteeClient.PostAsJsonAsync("/api/hogares/aceptar-invitacion", new { Token = token });

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(410, problem!.Status);
        Assert.Equal("INVITATION_EXPIRED", problem.Title);
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
    private sealed record MiembroBody(Guid UsuarioId, string Nombre, string? Rol, List<string> Alergias);
    private sealed record AceptarInvitacionBody(Guid HogarId, string HogarNombre, string AccessToken);
}
