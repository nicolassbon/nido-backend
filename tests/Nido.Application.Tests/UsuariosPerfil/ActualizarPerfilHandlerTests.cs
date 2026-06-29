using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nido.Application.Auth.Register;
using Nido.Application.Common.ProfileImages;
using Nido.Application.Common.Storage;
using Nido.Application.UsuariosPerfil;
using Nido.Domain.Usuarios;

namespace Nido.Application.Tests.UsuariosPerfil;

public sealed class ActualizarPerfilHandlerTests
{
    [Fact]
    public async Task Handle_RemoveFoto_WithExternalUrl_DoesNotDeleteStorageObject()
    {
        var repository = new FakeUsuarioRepository(new Usuario(Guid.NewGuid(), "Nico", "nico@test.com", "U", fotoStorageKey: "https://lh3.googleusercontent.com/a/avatar"));
        var storage = new FakeFileStorageService();
        var handler = CreateHandler(repository, storage);

        await handler.HandleAsync(new ActualizarPerfilCommand(repository.Usuario.Id, "Nico", "U", null, null, RemoveFoto: true), CancellationToken.None);

        Assert.Equal(0, storage.DeleteCalls);
        Assert.Null(repository.UpdatedUsuario!.FotoStorageKey);
    }

    [Fact]
    public async Task Handle_RemoveFoto_WhenManagedStorageDeleteFails_LogsWarningAndContinues()
    {
        var repository = new FakeUsuarioRepository(new Usuario(Guid.NewGuid(), "Nico", "nico@test.com", "U", fotoStorageKey: "avatars/user.webp"));
        var storage = new FakeFileStorageService { ThrowOnDelete = true };
        var logger = new RecordingLogger<ActualizarPerfilHandler>();
        var handler = CreateHandler(repository, storage, logger);

        await handler.HandleAsync(new ActualizarPerfilCommand(repository.Usuario.Id, "Nico", "U", null, null, RemoveFoto: true), CancellationToken.None);

        Assert.Equal(1, storage.DeleteCalls);
        Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, logger.Entries[0].LogLevel);
        Assert.Contains("Profile image cleanup failed", logger.Entries[0].Message, StringComparison.Ordinal);
    }

    private static ActualizarPerfilHandler CreateHandler(
        FakeUsuarioRepository repository,
        FakeFileStorageService storage,
        ILogger<ActualizarPerfilHandler>? logger = null)
        => new(
            repository,
            new FakeProfileImageProcessor(),
            storage,
            new StorageKeyFactory(),
            logger ?? NullLogger<ActualizarPerfilHandler>.Instance);

    private sealed class FakeUsuarioRepository(Usuario usuario) : IUsuarioRepository
    {
        public Usuario Usuario { get; } = usuario;
        public Usuario? UpdatedUsuario { get; private set; }

        public Task<Usuario?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult<Usuario?>(id == Usuario.Id ? Usuario : null);

        public Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken)
        {
            UpdatedUsuario = usuario;
            return Task.CompletedTask;
        }

        public Task<PerfilStatsResult> GetStatsAsync(Guid usuarioId, Guid hogarId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<string>> GetRestriccionesUsuarioAsync(Guid usuarioId, string tipo, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task ReplaceRestriccionesUsuarioAsync(Guid usuarioId, string tipo, IReadOnlyList<Guid> restriccionIds, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class FakeProfileImageProcessor : IProfileImageProcessor
    {
        public Task<ProcessedProfileImage> ProcessAsync(RegistrationProfileImageUpload upload, CancellationToken cancellationToken)
            => Task.FromResult(new ProcessedProfileImage([1, 2, 3], "image/webp", 1, 1, 3));
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public int DeleteCalls { get; private set; }
        public bool ThrowOnDelete { get; set; }

        public Task<FileStorageUploadResult> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(string fileName, CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            if (ThrowOnDelete)
            {
                throw new InvalidOperationException("delete failed");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }
    }

    private sealed record LogEntry(LogLevel LogLevel, string Message);
}
