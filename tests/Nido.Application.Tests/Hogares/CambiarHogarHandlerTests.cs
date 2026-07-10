using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Auth.RefreshToken;
using Nido.Application.Hogares;
using Nido.Application.Hogares.Exceptions;
using Nido.Application.Payments;

namespace Nido.Application.Tests.Hogares;

public sealed class CambiarHogarHandlerTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid HogarId   = Guid.NewGuid();

    [Fact]
    public async Task Handle_UsuarioEsMiembro_RetornaTokenNuevo()
    {
        var invRepo   = new FakeInvitacionRepository(esMiembro: true);
        var hogarRepo = new FakeHogarRepository(new HogarInfo(HogarId, "Casa de verano"));
        var handler   = new CambiarHogarHandler(invRepo, hogarRepo, new FakeJwt());

        var result = await handler.Handle(
            new CambiarHogarCommand(UsuarioId, HogarId),
            CancellationToken.None);

        Assert.Equal(HogarId, result.HogarId);
        Assert.Equal("Casa de verano", result.HogarNombre);
        Assert.Equal("nuevo-jwt", result.NuevoToken);
    }

    [Fact]
    public async Task Handle_UsuarioNoEsMiembro_LanzaNotHouseholdMember()
    {
        var invRepo   = new FakeInvitacionRepository(esMiembro: false);
        var hogarRepo = new FakeHogarRepository(new HogarInfo(HogarId, "Casa de verano"));
        var handler   = new CambiarHogarHandler(invRepo, hogarRepo, new FakeJwt());

        await Assert.ThrowsAsync<NotHouseholdMemberException>(() =>
            handler.Handle(new CambiarHogarCommand(UsuarioId, HogarId), CancellationToken.None));
    }

    private sealed class FakeHogarRepository(HogarInfo hogar) : IHogarRepository
    {
        public Task<HogarInfo?> GetByIdAsync(Guid hogarId, CancellationToken ct)
            => Task.FromResult<HogarInfo?>(hogar);

        public Task UpdateNombreAsync(Guid hogarId, string nombre, CancellationToken ct) => Task.CompletedTask;
        public Task<HogarInfo> CreateHogarAsync(Guid uid, string nombre, CancellationToken ct)
            => Task.FromResult(new HogarInfo(Guid.NewGuid(), nombre));
        public Task<List<HogarConRolInfo>> GetUserHogaresAsync(Guid uid, CancellationToken ct) => Task.FromResult(new List<HogarConRolInfo>());
        public Task DeleteHogarAsync(Guid hogarId, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeInvitacionRepository(bool esMiembro) : IInvitacionRepository
    {
        public Task<bool> IsMemberOfHouseholdAsync(Guid u, Guid h, CancellationToken ct)
            => Task.FromResult(esMiembro);

        public Task<(string Email, string Nombre)> GetUsuarioInfoAsync(Guid u, CancellationToken ct)
            => Task.FromResult(("user@test.com", "Usuario Test"));

        public Task<bool> IsUserHouseholdOwnerAsync(Guid u, Guid h, CancellationToken ct) => Task.FromResult(true);
        public Task RemoveMiembroAsync(Guid h, Guid u, CancellationToken ct) => Task.CompletedTask;
        public Task<int> CountRealMembersAsync(Guid h, CancellationToken ct) => Task.FromResult(0);
        public Task<string> CreateInvitacionAsync(Guid h, Guid i, string e, DateTime ex, CancellationToken ct) => Task.FromResult("");
        public Task<InvitacionInfo?> GetInvitacionByTokenAsync(string t, CancellationToken ct) => Task.FromResult<InvitacionInfo?>(null);
        public Task<bool> IsUserInAnyHouseholdAsync(Guid u, CancellationToken ct) => Task.FromResult(false);
        public Task AddUserToHouseholdAsync(Guid u, Guid h, string t, CancellationToken ct) => Task.CompletedTask;
        public Task<List<MiembroInfo>> GetMiembrosAsync(Guid h, CancellationToken ct) => Task.FromResult(new List<MiembroInfo>());
    }

    private sealed class FakeJwt : IJwtTokenService
    {
        public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre) => "nuevo-jwt";
        public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdPlan plan) => CreateToken(usuarioId, hogarId, email, nombre);
        public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdEntitlement entitlement) => CreateToken(usuarioId, hogarId, email, nombre);
        public string GenerateRefreshToken() => "refresh";
        public string HashRefreshToken(string refreshToken) => $"hash:{refreshToken}";
        public (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre)
            => ("nuevo-jwt", "refresh", DateTime.UtcNow.AddDays(7));
        public (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdPlan plan)
            => CreateAuthTokens(usuarioId, hogarId, email, nombre);
        public (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre, HouseholdEntitlement entitlement)
            => CreateAuthTokens(usuarioId, hogarId, email, nombre);
    }
}
