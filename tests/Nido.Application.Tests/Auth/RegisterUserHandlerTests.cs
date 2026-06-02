using Nido.Application.Auth.Register;
using Nido.Application.Auth.ResetPassword;
using Nido.Application.Auth.Register;
using Nido.Application.Auth;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Auth.RefreshToken;
using Nido.Application.Auth.Exceptions;
using Nido.Application.Common.ProfileImages;
using Microsoft.Extensions.Logging.Abstractions;

namespace Nido.Application.Tests.Auth;

public sealed class RegisterUserHandlerTests
{
    [Fact]
    public async Task Handle_CreatesUserAndReturnsToken()
    {
        var repo = new FakeAuthRepository();
        var handler = new RegisterUserHandler(repo, new FakeHasher(), new FakeJwt(), new FakeProfileImageProcessor(), new FakeProfileImageStorage(), NullLogger<RegisterUserHandler>.Instance);

        var result = await handler.Handle(new RegisterUserCommand("Nico", "nico@mail.com", "Password1", "M", null), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.UsuarioId);
        Assert.NotEqual(Guid.Empty, result.HogarId);
        Assert.Equal("token", result.AccessToken);
        Assert.Equal("hashed:Password1", repo.StoredHash);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsEmailAlreadyExists()
    {
        var repo = new FakeAuthRepository
        {
            ExistingUser = new User(Guid.NewGuid(), "Test", "nico@mail.com", "hashed:Old", null, null)
        };
        var handler = new RegisterUserHandler(repo, new FakeHasher(), new FakeJwt(), new FakeProfileImageProcessor(), new FakeProfileImageStorage(), NullLogger<RegisterUserHandler>.Instance);

        var ex = await Assert.ThrowsAsync<EmailAlreadyExistsException>(() =>
            handler.Handle(new RegisterUserCommand("Nico", "nico@mail.com", "Password1", "M", null), CancellationToken.None));

        Assert.Equal("EMAIL_ALREADY_EXISTS", ex.Code);
        Assert.Equal("Email already exists.", ex.Message);
    }

    [Fact]
    public async Task Handle_GoogleOnlyUser_AddsPasswordAndReturnsTokens()
    {
        var userId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var repo = new FakeAuthRepository
        {
            ExistingUser = new User(userId, "Test", "nico@mail.com", null, "google", "google-id-1"),
            HogarId = hogarId
        };
        var handler = new RegisterUserHandler(repo, new FakeHasher(), new FakeJwt(), new FakeProfileImageProcessor(), new FakeProfileImageStorage(), NullLogger<RegisterUserHandler>.Instance);

        var result = await handler.Handle(new RegisterUserCommand("Nico", "nico@mail.com", "Password1", "M", null), CancellationToken.None);

        Assert.Equal(userId, result.UsuarioId);
        Assert.Equal(hogarId, result.HogarId);
        Assert.Equal("token", result.AccessToken);
        Assert.Equal("refresh", result.RefreshToken);
        Assert.NotNull(repo.LastUpdatedUser);
        Assert.Equal("hashed:Password1", repo.LastUpdatedUser!.PasswordHash);
        Assert.NotNull(repo.StoredRefreshTokenHash);
    }

    [Fact]
    public async Task Handle_GoogleOnlyUser_PersistsRefreshToken()
    {
        var repo = new FakeAuthRepository
        {
            ExistingUser = new User(Guid.NewGuid(), "Test", "nico@mail.com", null, "google", "google-id-1"),
            HogarId = Guid.NewGuid()
        };
        var handler = new RegisterUserHandler(repo, new FakeHasher(), new FakeJwt(), new FakeProfileImageProcessor(), new FakeProfileImageStorage(), NullLogger<RegisterUserHandler>.Instance);

        await handler.Handle(new RegisterUserCommand("Nico", "nico@mail.com", "Password1", "M", null), CancellationToken.None);

        Assert.Equal("hash:refresh", repo.StoredRefreshTokenHash);
    }

    [Fact]
    public async Task Handle_MissingFields_ThrowsMissingRegistrationFields()
    {
        var repo = new FakeAuthRepository();
        var handler = new RegisterUserHandler(repo, new FakeHasher(), new FakeJwt(), new FakeProfileImageProcessor(), new FakeProfileImageStorage(), NullLogger<RegisterUserHandler>.Instance);

        var ex = await Assert.ThrowsAsync<MissingRegistrationFieldsException>(() =>
            handler.Handle(new RegisterUserCommand("", "nico@mail.com", "Password1", "M", null), CancellationToken.None));

        Assert.Equal("MISSING_REGISTRATION_FIELDS", ex.Code);
        Assert.Contains("Nombre", ex.Message);
    }

    [Fact]
    public async Task Handle_WeakPassword_ThrowsWeakPassword()
    {
        var repo = new FakeAuthRepository();
        var handler = new RegisterUserHandler(repo, new FakeHasher(), new FakeJwt(), new FakeProfileImageProcessor(), new FakeProfileImageStorage(), NullLogger<RegisterUserHandler>.Instance);

        var ex = await Assert.ThrowsAsync<WeakPasswordException>(() =>
            handler.Handle(new RegisterUserCommand("Nico", "nico@mail.com", "short", "M", null), CancellationToken.None));

        Assert.Equal("WEAK_PASSWORD", ex.Code);
    }

    [Fact]
    public async Task Handle_GoogleOnlyUserNoHousehold_ThrowsNoHouseholdAssociatedException()
    {
        var repo = new FakeAuthRepository
        {
            ExistingUser = new User(Guid.NewGuid(), "Test", "nico@mail.com", null, "google", "google-id-1"),
            HogarId = null
        };
        var handler = new RegisterUserHandler(repo, new FakeHasher(), new FakeJwt(), new FakeProfileImageProcessor(), new FakeProfileImageStorage(), NullLogger<RegisterUserHandler>.Instance);

        var ex = await Assert.ThrowsAsync<NoHouseholdAssociatedException>(() =>
            handler.Handle(new RegisterUserCommand("Nico", "nico@mail.com", "Password1", "M", null), CancellationToken.None));

        Assert.Equal("NO_HOUSEHOLD_ASSOCIATED", ex.Code);
    }

    [Fact]
    public async Task Handle_WithProfileImage_UploadsBeforePersistingMetadata()
    {
        var repo = new FakeAuthRepository();
        var storage = new FakeProfileImageStorage();
        var processor = new FakeProfileImageProcessor();
        var handler = new RegisterUserHandler(repo, new FakeHasher(), new FakeJwt(), processor, storage, NullLogger<RegisterUserHandler>.Instance);

        var foto = new RegistrationProfileImageUpload("avatar.png", "image/png", [1, 2, 3, 4]);
        var result = await handler.Handle(new RegisterUserCommand("Nico", "foto@mail.com", "Password1", "M", foto), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.UsuarioId);
        Assert.Equal(1, storage.UploadCalls);
        Assert.NotNull(repo.LastProfileImage);
        Assert.Equal("image/webp", repo.LastProfileImage!.ContentType);
        Assert.Contains($"usuarios/{result.UsuarioId}/profile/", repo.LastProfileImage.StorageKey, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handle_WhenUploadFails_DoesNotCreateUser()
    {
        var repo = new FakeAuthRepository();
        var storage = new FakeProfileImageStorage { ThrowOnUpload = true };
        var handler = new RegisterUserHandler(repo, new FakeHasher(), new FakeJwt(), new FakeProfileImageProcessor(), storage, NullLogger<RegisterUserHandler>.Instance);

        var foto = new RegistrationProfileImageUpload("avatar.png", "image/png", [1, 2, 3, 4]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RegisterUserCommand("Nico", "upload-fail@mail.com", "Password1", "M", foto), CancellationToken.None));

        Assert.Equal(0, repo.CreateCalls);
        Assert.Equal(0, storage.DeleteCalls);
    }

    [Fact]
    public async Task Handle_WhenPersistenceFailsAfterUpload_DeletesUploadedObject()
    {
        var repo = new FakeAuthRepository { ThrowOnCreate = true };
        var storage = new FakeProfileImageStorage();
        var handler = new RegisterUserHandler(repo, new FakeHasher(), new FakeJwt(), new FakeProfileImageProcessor(), storage, NullLogger<RegisterUserHandler>.Instance);

        var foto = new RegistrationProfileImageUpload("avatar.png", "image/png", [1, 2, 3, 4]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RegisterUserCommand("Nico", "persist-fail@mail.com", "Password1", "M", foto), CancellationToken.None));

        Assert.Equal(1, storage.UploadCalls);
        Assert.Equal(1, storage.DeleteCalls);
        Assert.Equal(storage.LastUploadedStorageKey, storage.LastDeletedStorageKey);
    }

    [Fact]
    public async Task Handle_WhenDeleteFailsAfterPersistenceFailure_RethrowsOriginalPersistenceError()
    {
        var repo = new FakeAuthRepository { ThrowOnCreate = true };
        var storage = new FakeProfileImageStorage { ThrowOnDelete = true };
        var handler = new RegisterUserHandler(repo, new FakeHasher(), new FakeJwt(), new FakeProfileImageProcessor(), storage, NullLogger<RegisterUserHandler>.Instance);

        var foto = new RegistrationProfileImageUpload("avatar.png", "image/png", [1, 2, 3, 4]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RegisterUserCommand("Nico", "cleanup-fail@mail.com", "Password1", "M", foto), CancellationToken.None));

        Assert.Equal("persistence failed", exception.Message);
        Assert.Equal(1, storage.DeleteCalls);
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public User? ExistingUser { get; set; }
        public string StoredHash { get; private set; } = string.Empty;
        public Guid? HogarId { get; set; }
        public User? LastUpdatedUser { get; private set; }
        public string? StoredRefreshTokenHash { get; private set; }
        public int CreateCalls { get; private set; }
        public bool ThrowOnCreate { get; set; }
        public UserProfileImageMetadata? LastProfileImage { get; private set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) => Task.FromResult(ExistingUser is not null);

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithGoogleAsync(CreateOAuthUserData data, CancellationToken cancellationToken)
            => Task.FromResult((Guid.NewGuid(), Guid.NewGuid()));

        public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithPasswordAsync(Guid usuarioId, Guid hogarId, string nombre, string email, string passwordHash, string sexo, UserProfileImageMetadata? profileImage, CancellationToken cancellationToken)
        {
            CreateCalls++;
            LastProfileImage = profileImage;
            if (ThrowOnCreate)
            {
                throw new InvalidOperationException("persistence failed");
            }

            StoredHash = passwordHash;
            return Task.FromResult((usuarioId, hogarId));
        }

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult(ExistingUser);

        public Task<User?> FindByGoogleIdAsync(string googleId, CancellationToken cancellationToken) => Task.FromResult<User?>(null);

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(ExistingUser);

        public Task AddRefreshTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken)
        {
            StoredRefreshTokenHash = tokenHash;
            return Task.CompletedTask;
        }

        public Task<RefreshTokenInfo?> GetValidRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult<RefreshTokenInfo?>(null);

        public Task SavePasswordResetTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<PasswordResetTokenInfo?> GetValidPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult<PasswordResetTokenInfo?>(null);

        public Task RemoveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ConsumePasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateUserPasswordAsync(Guid usuarioId, string passwordHash, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateUserAsync(User user, CancellationToken cancellationToken)
        {
            LastUpdatedUser = user;
            return Task.CompletedTask;
        }

        public Task<Guid?> GetUserHogarIdAsync(Guid usuarioId, CancellationToken cancellationToken)
            => Task.FromResult<Guid?>(HogarId);
    }

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string password, string hash) => hash == $"hashed:{password}";
    }

    private sealed class FakeJwt : IJwtTokenService
    {
        public string CreateToken(Guid usuarioId, Guid hogarId, string email, string nombre) => "token";
        public string GenerateRefreshToken() => "refresh";
        public string HashRefreshToken(string refreshToken) => $"hash:{refreshToken}";
        public (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt) CreateAuthTokens(Guid usuarioId, Guid hogarId, string email, string nombre)
            => ("token", "refresh", DateTime.UtcNow.AddDays(7));
    }

    private sealed class FakeProfileImageProcessor : IProfileImageProcessor
    {
        public Task<ProcessedProfileImage> ProcessAsync(RegistrationProfileImageUpload upload, CancellationToken cancellationToken)
            => Task.FromResult(new ProcessedProfileImage(upload.Content, "image/webp", 100, 100, upload.Content.Length));
    }

    private sealed class FakeProfileImageStorage : IProfileImageStorage
    {
        public int UploadCalls { get; private set; }
        public int DeleteCalls { get; private set; }
        public bool ThrowOnUpload { get; set; }
        public bool ThrowOnDelete { get; set; }
        public string? LastUploadedStorageKey { get; private set; }
        public string? LastDeletedStorageKey { get; private set; }

        public Task UploadAsync(string storageKey, byte[] content, string contentType, CancellationToken cancellationToken)
        {
            UploadCalls++;
            LastUploadedStorageKey = storageKey;
            if (ThrowOnUpload)
            {
                throw new InvalidOperationException("upload failed");
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            DeleteCalls++;
            LastDeletedStorageKey = storageKey;
            if (ThrowOnDelete)
            {
                throw new InvalidOperationException("delete failed");
            }

            return Task.CompletedTask;
        }
    }
}
