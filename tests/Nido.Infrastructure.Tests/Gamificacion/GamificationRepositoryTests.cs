using Microsoft.EntityFrameworkCore;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Nido.Application.Gamificacion;
using Nido.Tests.Shared;
using Nido.Infrastructure.Gamificacion;

namespace Nido.Infrastructure.Tests.Gamificacion;

public sealed class GamificationRepositoryTests : IAsyncDisposable
{
    private readonly PostgresTestDatabase _db;
    private readonly NidoDbContext _context;

    public GamificationRepositoryTests()
    {
        _db = PostgresTestServer.GetSharedAsync().GetAwaiter().GetResult()
            .CreateDatabaseAsync("gamif_repo_tests").GetAwaiter().GetResult();

        var options = new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(_db.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .Options;

        _context = new NidoDbContext(options);
        _context.Database.MigrateAsync().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _db.DisposeAsync();
    }

    private async Task SeedUser(NidoDbContext db, Guid usuarioId, string nombre, string email)
    {
        var usuario = new Usuario
        {
            Id = usuarioId,
            Nombre = nombre,
            Email = email,
            Sexo = "F",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            AlertaVencimientoDias = 3,
            TemaPreferido = "system",
        };
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
    }

    private async Task SeedHousehold(NidoDbContext db, Guid hogarId, Guid usuarioId, string nombre, string? nombreRepresentado = null)
    {
        // Seed household
        await db.Database.ExecuteSqlAsync(
            $"INSERT INTO hogares (id, nombre, created_at, modo_ahorro) VALUES ({hogarId}, {nombre}, NOW(), false) ON CONFLICT DO NOTHING");

        // Seed membership
        await db.Database.ExecuteSqlAsync(
            $"INSERT INTO miembros_hogar (id, usuario_id, hogar_id, rol, nombre_representado) VALUES ({Guid.NewGuid()}, {usuarioId}, {hogarId}, 'admin', {nombreRepresentado}) ON CONFLICT DO NOTHING");
    }

    private async Task SeedTask(NidoDbContext db, Guid tareaId, Guid hogarId, Guid creadoPor,
        string titulo, string estado, Guid? completadoPor = null)
    {
        if (completadoPor.HasValue)
        {
            await db.Database.ExecuteSqlAsync(
                $"INSERT INTO tareas (id, hogar_id, creado_por, titulo, estado, completado_por, fecha_completado, created_at) VALUES ({tareaId}, {hogarId}, {creadoPor}, {titulo}, {estado}, {completadoPor.Value}, NOW(), NOW()) ON CONFLICT DO NOTHING");
            return;
        }

        await db.Database.ExecuteSqlAsync(
            $"INSERT INTO tareas (id, hogar_id, creado_por, titulo, estado, completado_por, fecha_completado, created_at) VALUES ({tareaId}, {hogarId}, {creadoPor}, {titulo}, {estado}, NULL, NULL, NOW()) ON CONFLICT DO NOTHING");
    }

    [Fact]
    public async Task CountCurrentlyCompletedTasksAsync_OnlyCountsActiveMemberships()
    {
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        await SeedUser(_context, usuarioId, "TestUser", "test@example.com");
        await SeedHousehold(_context, hogarId, usuarioId, "TestHogar");

        // Seed 3 completed tasks for this user in the active household
        for (int i = 0; i < 3; i++)
            await SeedTask(_context, Guid.NewGuid(), hogarId, usuarioId, $"Task {i}", "completada", usuarioId);

        // Seed a task completed by another user (should not count)
        var otherUserId = Guid.NewGuid();
        await SeedUser(_context, otherUserId, "OtherUser", "other@example.com");
        // Add other user to household
        await _context.Database.ExecuteSqlAsync(
            $"INSERT INTO miembros_hogar (id, usuario_id, hogar_id, rol) VALUES ({Guid.NewGuid()}, {otherUserId}, {hogarId}, 'member') ON CONFLICT DO NOTHING");
        await SeedTask(_context, Guid.NewGuid(), hogarId, otherUserId, "Other Task", "completada", otherUserId);

        // Seed a represented/inactive membership for the same user (should not count)
        var representedHogarId = Guid.NewGuid();
        await SeedHousehold(_context, representedHogarId, usuarioId, "Represented household", "Child profile");
        await SeedTask(_context, Guid.NewGuid(), representedHogarId, usuarioId, "Represented Task", "completada", usuarioId);

        // Seed a historical task in a household without a current membership (should not count)
        var formerHogarId = Guid.NewGuid();
        await _context.Database.ExecuteSqlAsync(
            $"INSERT INTO hogares (id, nombre, created_at, modo_ahorro) VALUES ({formerHogarId}, {"Former household"}, NOW(), false) ON CONFLICT DO NOTHING");
        await SeedTask(_context, Guid.NewGuid(), formerHogarId, usuarioId, "Former Household Task", "completada", usuarioId);

        var repo = new GamificationRepository(_context);

        var count = await repo.CountCurrentlyCompletedTasksAsync(usuarioId, CancellationToken.None);

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetUnlockedLevelsAsync_ReturnsPersistedLevels()
    {
        var usuarioId = Guid.NewGuid();
        await SeedUser(_context, usuarioId, "LevelUser", "level@example.com");

        // Directly insert unlock rows
        await _context.Database.ExecuteSqlAsync(
            $"INSERT INTO gamificacion_niveles_desbloqueados (id, usuario_id, nivel, desbloqueado_en) VALUES ({Guid.NewGuid()}, {usuarioId}, 1, NOW())");
        await _context.Database.ExecuteSqlAsync(
            $"INSERT INTO gamificacion_niveles_desbloqueados (id, usuario_id, nivel, desbloqueado_en) VALUES ({Guid.NewGuid()}, {usuarioId}, 3, NOW())");

        var repo = new GamificationRepository(_context);

        var levels = await repo.GetUnlockedLevelsAsync(usuarioId, CancellationToken.None);

        Assert.Equal(2, levels.Count);
        Assert.Contains(1, levels);
        Assert.Contains(3, levels);
    }

    [Fact]
    public async Task InsertMissingUnlocksAsync_InsertsAllMissingLevels_AndReturnsOnlyNewlyInserted()
    {
        var usuarioId = Guid.NewGuid();
        await SeedUser(_context, usuarioId, "InsertUser", "insert@example.com");

        // Pre-insert level 1
        await _context.Database.ExecuteSqlAsync(
            $"INSERT INTO gamificacion_niveles_desbloqueados (id, usuario_id, nivel, desbloqueado_en) VALUES ({Guid.NewGuid()}, {usuarioId}, 1, NOW())");

        var repo = new GamificationRepository(_context);

        var newlyInserted = await repo.InsertMissingUnlocksAsync(
            usuarioId, new[] { 1, 2, 3, 4 }, DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(3, newlyInserted.Count);
        Assert.Contains(2, newlyInserted);
        Assert.Contains(3, newlyInserted);
        Assert.Contains(4, newlyInserted);
        Assert.DoesNotContain(1, newlyInserted); // already existed

        // Verify they are actually persisted
        var allLevels = await repo.GetUnlockedLevelsAsync(usuarioId, CancellationToken.None);
        Assert.Equal(4, allLevels.Count);
    }

    [Fact]
    public async Task InsertMissingUnlocksAsync_OnUniqueConflict_TreatsAsSuccessNoOp_AndReturnsEmpty()
    {
        var usuarioId = Guid.NewGuid();
        await SeedUser(_context, usuarioId, "ConflictUser", "conflict@example.com");

        // Already insert level 5
        await _context.Database.ExecuteSqlAsync(
            $"INSERT INTO gamificacion_niveles_desbloqueados (id, usuario_id, nivel, desbloqueado_en) VALUES ({Guid.NewGuid()}, {usuarioId}, 5, NOW())");

        var repo = new GamificationRepository(_context);

        // Try inserting level 5 again — should be no-op, return empty
        var newlyInserted = await repo.InsertMissingUnlocksAsync(
            usuarioId, new[] { 5 }, DateTime.UtcNow, CancellationToken.None);

        Assert.Empty(newlyInserted);

        // Still only one row
        var allLevels = await repo.GetUnlockedLevelsAsync(usuarioId, CancellationToken.None);
        Assert.Single(allLevels);
        Assert.Contains(5, allLevels);
    }

    [Fact]
    public async Task InsertMissingUnlocksAsync_ConcurrentDuplicateInsert_DoesNotThrow_AndReturnsNewlyInsertedOnly()
    {
        var usuarioId = Guid.NewGuid();
        await SeedUser(_context, usuarioId, "ConcurrentUser", "concurrent@example.com");

        var options = new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(_db.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .Options;

        await using var context1 = new NidoDbContext(options);
        await using var context2 = new NidoDbContext(options);
        var repo1 = new GamificationRepository(context1);
        var repo2 = new GamificationRepository(context2);

        // Simulate two concurrent inserts for same user/levels through independent unit-of-work contexts.
        var task1 = repo1.InsertMissingUnlocksAsync(
            usuarioId, new[] { 10, 20, 30 }, DateTime.UtcNow, CancellationToken.None);
        var task2 = repo2.InsertMissingUnlocksAsync(
            usuarioId, new[] { 10, 20, 30 }, DateTime.UtcNow, CancellationToken.None);

        var results = await Task.WhenAll(task1, task2);

        // Neither should throw
        var r1 = results[0];
        var r2 = results[1];

        // Total newly inserted unique levels should be 3 across both
        var allInserted = new HashSet<int>(r1.Concat(r2));
        Assert.Equal(3, allInserted.Count);

        // Verify persistence
        var allLevels = await repo1.GetUnlockedLevelsAsync(usuarioId, CancellationToken.None);
        Assert.Equal(3, allLevels.Count);
    }
}
