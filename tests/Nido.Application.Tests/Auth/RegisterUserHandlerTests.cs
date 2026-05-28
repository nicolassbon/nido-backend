using Nido.Application.Auth;

namespace Nido.Application.Tests.Auth;

public sealed class RegisterUserHandlerTests
{
    [Fact]
    public async Task Handle_CreatesUserAndReturnsToken()
    {
        var repo = new FakeAuthRepository();
        var handler = new RegisterUserHandler(repo, new FakeHasher(), new FakeJwt());

        var result = await handler.Handle(new RegisterUserCommand("Nico", "nico@mail.com", "Password1", "M", null), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.UsuarioId);
        Assert.NotEqual(Guid.Empty, result.HogarId);
        Assert.Equal("token", result.AccessToken);
        Assert.Equal("hashed:Password1", repo.StoredHash);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_Throws()
    {
        var repo = new FakeAuthRepository { Existing = true };
        var handler = new RegisterUserHandler(repo, new FakeHasher(), new FakeJwt());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RegisterUserCommand("Nico", "nico@mail.com", "Password1", "M", null), CancellationToken.None));
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public bool Existing { get; set; }
        public string StoredHash { get; private set; } = string.Empty;

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => Task.FromResult(Existing);

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithDefaultHouseholdAsync(string nombre, string email, string passwordHash, string sexo, string? fotoUrl, CancellationToken cancellationToken)
        {
            StoredHash = passwordHash;
            return Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));
        }
    }

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";
    }

    private sealed class FakeJwt : IJwtTokenService
    {
        public string CreateToken(Guid usuarioId, Guid hogarId, string email) => "token";
    }
}
