using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nido.Application.Common.Security;
using Nido.Infrastructure.Hogares;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Nido.Tests.Shared;

namespace Nido.Infrastructure.Tests.Hogares;

public sealed class HogarMembershipRepositoryTests : IAsyncLifetime
{
    private PostgresTestDatabase? _testDatabase;
    private NidoDbContext? _dbContext;
    private IHogarMembershipRepository? _repository;

    private NidoDbContext DbContext => _dbContext ?? throw new InvalidOperationException("Test database not initialized.");
    private IHogarMembershipRepository Repository => _repository ?? throw new InvalidOperationException("Repository not initialized.");

    public async Task InitializeAsync()
    {
        var server = await PostgresTestServer.GetSharedAsync();
        _testDatabase = await server.CreateDatabaseAsync(nameof(HogarMembershipRepositoryTests));

        var options = new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(_testDatabase.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        _dbContext = new NidoDbContext(options);
        await _dbContext.Database.MigrateAsync();
        _repository = new HogarMembershipRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        if (_dbContext is not null)
        {
            await _dbContext.DisposeAsync();
        }

        if (_testDatabase is not null)
        {
            await _testDatabase.DisposeAsync();
        }
    }

    [Fact]
    public async Task IsOwnerAsync_WhenRealOwnerExists_ReturnsTrue()
    {
        var (userId, hogarId) = await SeedMembershipAsync("owner", representedName: null);

        var result = await Repository.IsOwnerAsync(userId, hogarId, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task IsMemberAsync_WhenOnlyRepresentedMemberExists_ReturnsFalse()
    {
        var (userId, hogarId) = await SeedMembershipAsync("owner", representedName: "Pepe");

        var result = await Repository.IsMemberAsync(userId, hogarId, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task IsOwnerAsync_WhenOnlyRepresentedOwnerExists_ReturnsFalse()
    {
        var (userId, hogarId) = await SeedMembershipAsync("owner", representedName: "Pepe");

        var result = await Repository.IsOwnerAsync(userId, hogarId, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task IsInAnyHouseholdAsync_WhenOnlyRepresentedMembershipExists_ReturnsFalse()
    {
        var (userId, _) = await SeedMembershipAsync("conviviente", representedName: "Pepe");

        var result = await Repository.IsInAnyHouseholdAsync(userId, CancellationToken.None);

        Assert.False(result);
    }

    private async Task<(Guid UserId, Guid HogarId)> SeedMembershipAsync(string role, string? representedName)
    {
        var userId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();

        DbContext.Usuarios.Add(new Usuario
        {
            Id = userId,
            Nombre = "Test User",
            Email = $"{Guid.NewGuid():N}@test.com",
            Sexo = "U",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        DbContext.Hogares.Add(new Hogare
        {
            Id = hogarId,
            Nombre = "Test Hogar",
            CreatedAt = DateTime.UtcNow
        });

        DbContext.MiembrosHogars.Add(new MiembrosHogar
        {
            Id = Guid.NewGuid(),
            UsuarioId = userId,
            HogarId = hogarId,
            Rol = role,
            NombreRepresentado = representedName,
            Puntos = 0
        });

        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        return (userId, hogarId);
    }
}
