using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nido.Application.ListaCompras;
using Nido.Application.Telegram.Authorization;
using Nido.Application.Telegram.Menu;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Nido.Infrastructure.Telegram.Menu;
using Nido.Tests.Shared;
using Xunit;

namespace Nido.Infrastructure.Tests.Telegram;

public sealed class TelegramMenuInfrastructureTests : IAsyncLifetime
{
    private readonly PostgresTestServer _server = PostgresTestServer.GetSharedAsync().GetAwaiter().GetResult();
    private PostgresTestDatabase _database = null!;
    private NidoDbContext _db = null!;
    private EfTelegramMenuReadService _readService = null!;

    public async Task InitializeAsync()
    {
        _database = await _server.CreateDatabaseAsync("telegram_menu_provider_tests");

        var options = new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        _db = new NidoDbContext(options);
        await _db.Database.MigrateAsync();

        _readService = new EfTelegramMenuReadService(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public void Registry_GetDefaultMenu_ReturnsFiveNumberedOptions()
    {
        var registry = new InMemoryTelegramMenuRegistry();

        var menu = registry.GetDefaultMenu();

        Assert.Equal("main-menu", menu.Id);
        Assert.Collection(
            menu.Options,
            option => Assert.Equal("1", option.Key),
            option => Assert.Equal("2", option.Key),
            option => Assert.Equal("3", option.Key),
            option => Assert.Equal("4", option.Key),
            option => Assert.Equal("5", option.Key));
    }

    [Fact]
    public async Task GetExpiringStockAsync_WhenNoData_ReturnsEmptyResultWithoutOverflow()
    {
        var link = await SeedLinkedUserAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var result = await _readService.GetExpiringStockAsync(link.HogarId, today, days: 7, limit: 8, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.False(result.HasMore);
        Assert.Equal(0, result.RemainingCount);
    }

    [Fact]
    public async Task GetExpiringStockAsync_ReturnsItemsWithinWindowAndExcludesDistantExpirations()
    {
        var link = await SeedLinkedUserAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var category = new CategoriasProducto { Id = Guid.NewGuid(), Nombre = $"Lácteos {Guid.NewGuid():N}" };
        var product = new Producto { Id = Guid.NewGuid(), Nombre = "Yogur", CategoriaId = category.Id, Categoria = category };
        var otherProduct = new Producto { Id = Guid.NewGuid(), Nombre = "Arroz" };

        _db.CategoriasProductos.Add(category);
        _db.Productos.AddRange(product, otherProduct);
        _db.StockHogars.AddRange(
            CreateStock(link, product, quantity: 2, unit: "u", containers: 1, dueDate: today.AddDays(1)),
            CreateStock(link, otherProduct, quantity: 1, unit: "kg", containers: 1, dueDate: today.AddDays(12)),
            CreateStock(link, product, quantity: 3, unit: "u", containers: 1, dueDate: today));
        await _db.SaveChangesAsync();

        var result = await _readService.GetExpiringStockAsync(link.HogarId, today, days: 7, limit: 8, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Yogur", result.Items[0].ProductName);
        Assert.Equal(today, result.Items[0].DueDate);
        Assert.Equal("Yogur", result.Items[1].ProductName);
        Assert.Equal(today.AddDays(1), result.Items[1].DueDate);
        Assert.DoesNotContain(result.Items, item => item.ProductName == "Arroz");
        Assert.False(result.HasMore);
    }

    [Fact]
    public async Task GetExpiringStockAsync_RespectsLimitAndReportsOverflow()
    {
        var link = await SeedLinkedUserAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var category = new CategoriasProducto { Id = Guid.NewGuid(), Nombre = $"Cat {Guid.NewGuid():N}" };
        _db.CategoriasProductos.Add(category);

        for (var i = 0; i < 5; i++)
        {
            var product = new Producto { Id = Guid.NewGuid(), Nombre = $"P{i}", CategoriaId = category.Id, Categoria = category };
            _db.Productos.Add(product);
            _db.StockHogars.Add(CreateStock(link, product, quantity: 1, unit: "u", containers: 1, dueDate: today.AddDays(i + 1)));
        }
        await _db.SaveChangesAsync();

        var result = await _readService.GetExpiringStockAsync(link.HogarId, today, days: 7, limit: 3, CancellationToken.None);

        // The read service uses `limit + 1` to detect overflow. We know
        // there is at least one more row; the exact remaining count would
        // require a separate COUNT query that we deliberately avoid.
        Assert.Equal(3, result.Items.Count);
        Assert.True(result.HasMore);
        Assert.True(result.RemainingCount >= 1);
    }

    [Fact]
    public async Task GetExpiringStockAsync_IsolatesByHousehold()
    {
        var link = await SeedLinkedUserAsync();
        var otherHogar = new Hogare { Id = Guid.NewGuid(), Nombre = "Otro hogar", CreatedAt = DateTime.UtcNow };
        _db.Hogares.Add(otherHogar);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sharedCategory = new CategoriasProducto { Id = Guid.NewGuid(), Nombre = $"Cat {Guid.NewGuid():N}" };
        var product = new Producto { Id = Guid.NewGuid(), Nombre = "Yogur", CategoriaId = sharedCategory.Id, Categoria = sharedCategory };
        _db.CategoriasProductos.Add(sharedCategory);
        _db.Productos.Add(product);

        _db.StockHogars.AddRange(
            CreateStock(link, product, quantity: 1, unit: "u", containers: 1, dueDate: today.AddDays(1)),
            CreateStockForHogar(otherHogar.Id, link.UsuarioId, product, quantity: 9, unit: "u", containers: 1, dueDate: today.AddDays(1)));
        await _db.SaveChangesAsync();

        var result = await _readService.GetExpiringStockAsync(link.HogarId, today, days: 7, limit: 8, CancellationToken.None);

        var only = Assert.Single(result.Items);
        Assert.Equal("Yogur", only.ProductName);
        Assert.Equal(1m, only.Quantity);
    }

    [Fact]
    public async Task GetPantrySummaryAsync_WhenNoStock_ReturnsZeroTotalsAndEmptyLists()
    {
        var link = await SeedLinkedUserAsync();

        var summary = await _readService.GetPantrySummaryAsync(link.HogarId, categoryLimit: 4, productLimit: 5, CancellationToken.None);

        Assert.Equal(0, summary.TotalUnits);
        Assert.Equal(0, summary.DistinctProductCount);
        Assert.Equal(0, summary.DistinctCategoryCount);
        Assert.Empty(summary.TopCategories);
        Assert.Empty(summary.TopProducts);
    }

    [Fact]
    public async Task GetPantrySummaryAsync_AggregatesContainersByCategoryAndProduct()
    {
        var link = await SeedLinkedUserAsync();
        var dairy = new CategoriasProducto { Id = Guid.NewGuid(), Nombre = $"Lácteos {Guid.NewGuid():N}" };
        var pantry = new CategoriasProducto { Id = Guid.NewGuid(), Nombre = $"Despensa {Guid.NewGuid():N}" };
        var milk = new Producto { Id = Guid.NewGuid(), Nombre = "Leche", CategoriaId = dairy.Id, Categoria = dairy };
        var rice = new Producto { Id = Guid.NewGuid(), Nombre = "Arroz", CategoriaId = pantry.Id, Categoria = pantry };

        _db.CategoriasProductos.AddRange(dairy, pantry);
        _db.Productos.AddRange(milk, rice);
        _db.StockHogars.AddRange(
            CreateStock(link, milk, quantity: 1, unit: "lt", containers: 2),
            CreateStock(link, rice, quantity: 1, unit: "kg", containers: 1));
        await _db.SaveChangesAsync();

        var summary = await _readService.GetPantrySummaryAsync(link.HogarId, categoryLimit: 4, productLimit: 5, CancellationToken.None);

        Assert.Equal(3, summary.TotalUnits);
        Assert.Equal(2, summary.DistinctProductCount);
        Assert.Equal(2, summary.DistinctCategoryCount);
        var dairyLine = Assert.Single(summary.TopCategories, line => line.Name == dairy.Nombre);
        Assert.Equal(2, dairyLine.Count);
        var pantryLine = Assert.Single(summary.TopCategories, line => line.Name == pantry.Nombre);
        Assert.Equal(1, pantryLine.Count);
        var milkLine = Assert.Single(summary.TopProducts, line => line.Name == "Leche");
        Assert.Equal(2, milkLine.Count);
    }

    [Fact]
    public async Task GetExpiringStockAsync_WithNonPositiveLimit_Throws()
    {
        var link = await SeedLinkedUserAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _readService.GetExpiringStockAsync(link.HogarId, today, days: 7, limit: 0, CancellationToken.None));
    }

    [Fact]
    public async Task GetPantrySummaryAsync_WithNonPositiveLimit_Throws()
    {
        var link = await SeedLinkedUserAsync();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _readService.GetPantrySummaryAsync(link.HogarId, categoryLimit: 0, productLimit: 1, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _readService.GetPantrySummaryAsync(link.HogarId, categoryLimit: 1, productLimit: 0, CancellationToken.None));
    }

    [Fact]
    public async Task GetPendingShoppingItemsAsync_WithNonPositiveLimit_Throws()
    {
        var link = await SeedLinkedUserAsync();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _readService.GetPendingShoppingItemsAsync(link.HogarId, limit: 0, CancellationToken.None));
    }

    [Fact]
    public async Task GetPendingAssignedTasksAsync_WithNonPositiveLimit_Throws()
    {
        var link = await SeedLinkedUserAsync();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _readService.GetPendingAssignedTasksAsync(link.HogarId, link.UsuarioId, limit: 0, CancellationToken.None));
    }

    [Fact]
    public async Task GetPantrySummaryAsync_IsolatesByHousehold()
    {
        var link = await SeedLinkedUserAsync();
        var otherHogar = new Hogare { Id = Guid.NewGuid(), Nombre = "Otro hogar", CreatedAt = DateTime.UtcNow };
        _db.Hogares.Add(otherHogar);

        var product = new Producto { Id = Guid.NewGuid(), Nombre = "Azúcar" };
        _db.Productos.Add(product);
        _db.StockHogars.AddRange(
            CreateStock(link, product, quantity: 1, unit: "kg", containers: 1),
            CreateStockForHogar(otherHogar.Id, link.UsuarioId, product, quantity: 99, unit: "kg", containers: 1));
        await _db.SaveChangesAsync();

        var summary = await _readService.GetPantrySummaryAsync(link.HogarId, categoryLimit: 4, productLimit: 5, CancellationToken.None);

        Assert.Equal(1, summary.TotalUnits);
        Assert.Equal(1, summary.DistinctProductCount);
    }

    [Fact]
    public async Task GetPendingShoppingItemsAsync_WhenNoData_ReturnsEmptyResultWithoutOverflow()
    {
        var link = await SeedLinkedUserAsync();

        var result = await _readService.GetPendingShoppingItemsAsync(link.HogarId, limit: 12, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.False(result.HasMore);
        Assert.Equal(0, result.RemainingCount);
    }

    [Fact]
    public async Task GetPendingShoppingItemsAsync_IncludesOnlyNonPurchasedNonInventoriedItems()
    {
        var link = await SeedLinkedUserAsync();
        var list = new ListaCompraHogar
        {
            Id = Guid.NewGuid(),
            HogarId = link.HogarId,
            Nombre = "Principal",
            CreadaPor = link.UsuarioId,
            CreatedAt = DateTime.UtcNow
        };
        var product = new Producto { Id = Guid.NewGuid(), Nombre = "Arroz" };

        _db.ListasCompraHogar.Add(list);
        _db.Productos.Add(product);
        _db.ListaCompras.AddRange(
            new ListaCompra
            {
                Id = Guid.NewGuid(),
                HogarId = link.HogarId,
                ListaId = list.Id,
                AgregadoPor = link.UsuarioId,
                ProductoId = product.Id,
                Producto = product,
                ProductoNombreSnapshot = "Arroz",
                GrupoNombre = "Cena",
                Orden = 0,
                Cantidad = 1,
                Unidad = "kg",
                CreatedAt = DateTime.UtcNow,
                Comprado = false,
                AgregadoAlInventario = false
            },
            new ListaCompra
            {
                Id = Guid.NewGuid(),
                HogarId = link.HogarId,
                ListaId = list.Id,
                AgregadoPor = link.UsuarioId,
                NombreManual = "Bananas",
                ProductoNombreSnapshot = "Bananas",
                GrupoNombre = "Frutas",
                Orden = 1,
                Cantidad = 6,
                Unidad = "u",
                CreatedAt = DateTime.UtcNow,
                Comprado = false,
                AgregadoAlInventario = false
            },
            new ListaCompra
            {
                Id = Guid.NewGuid(),
                HogarId = link.HogarId,
                ListaId = list.Id,
                AgregadoPor = link.UsuarioId,
                ProductoNombreSnapshot = "Queso",
                GrupoNombre = "Cena",
                Orden = 2,
                Cantidad = 1,
                Unidad = "u",
                CreatedAt = DateTime.UtcNow,
                Comprado = true,
                CompradoEn = DateTime.UtcNow
            },
            new ListaCompra
            {
                Id = Guid.NewGuid(),
                HogarId = link.HogarId,
                ListaId = list.Id,
                AgregadoPor = link.UsuarioId,
                ProductoNombreSnapshot = "Yerba",
                GrupoNombre = "Productos agregados",
                Orden = 3,
                Cantidad = 1,
                Unidad = "kg",
                CreatedAt = DateTime.UtcNow,
                Comprado = false,
                AgregadoAlInventario = true
            });
        await _db.SaveChangesAsync();

        var result = await _readService.GetPendingShoppingItemsAsync(link.HogarId, limit: 12, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, item => item.Name == "Arroz" && item.GroupName == "Cena" && item.Quantity == 1m && item.Unit == "kg");
        Assert.Contains(result.Items, item => item.Name == "Bananas" && item.GroupName == "Frutas" && item.Quantity == 6m && item.Unit == "u");
        Assert.DoesNotContain(result.Items, item => item.Name == "Queso");
        Assert.DoesNotContain(result.Items, item => item.Name == "Yerba");
        Assert.False(result.HasMore);
    }

    [Fact]
    public async Task GetPendingShoppingItemsAsync_RespectsLimitAndReportsOverflow()
    {
        var link = await SeedLinkedUserAsync();
        var list = new ListaCompraHogar
        {
            Id = Guid.NewGuid(),
            HogarId = link.HogarId,
            Nombre = "Principal",
            CreadaPor = link.UsuarioId,
            CreatedAt = DateTime.UtcNow
        };
        _db.ListasCompraHogar.Add(list);

        for (var i = 0; i < 4; i++)
        {
            _db.ListaCompras.Add(new ListaCompra
            {
                Id = Guid.NewGuid(),
                HogarId = link.HogarId,
                ListaId = list.Id,
                AgregadoPor = link.UsuarioId,
                ProductoNombreSnapshot = $"Item {i}",
                GrupoNombre = "General",
                Orden = i,
                Cantidad = 1,
                Unidad = "u",
                CreatedAt = DateTime.UtcNow,
                Comprado = false,
                AgregadoAlInventario = false
            });
        }
        await _db.SaveChangesAsync();

        var result = await _readService.GetPendingShoppingItemsAsync(link.HogarId, limit: 2, CancellationToken.None);

        // `limit + 1` overflow probe: at least one more row exists; the
        // exact total would require a separate COUNT we deliberately avoid.
        Assert.Equal(2, result.Items.Count);
        Assert.True(result.HasMore);
        Assert.True(result.RemainingCount >= 1);
    }

    [Fact]
    public async Task GetPendingShoppingItemsAsync_ExcludesRemovedItems()
    {
        var link = await SeedLinkedUserAsync();
        var list = new ListaCompraHogar
        {
            Id = Guid.NewGuid(),
            HogarId = link.HogarId,
            Nombre = "Principal",
            CreadaPor = link.UsuarioId,
            CreatedAt = DateTime.UtcNow
        };
        _db.ListasCompraHogar.Add(list);

        _db.ListaCompras.AddRange(
            new ListaCompra
            {
                Id = Guid.NewGuid(),
                HogarId = link.HogarId,
                ListaId = list.Id,
                AgregadoPor = link.UsuarioId,
                ProductoNombreSnapshot = "Pendiente",
                GrupoNombre = "General",
                Orden = 0,
                Cantidad = 1,
                Unidad = "u",
                CreatedAt = DateTime.UtcNow,
                Comprado = false,
                AgregadoAlInventario = false
            },
            new ListaCompra
            {
                Id = Guid.NewGuid(),
                HogarId = link.HogarId,
                ListaId = list.Id,
                AgregadoPor = link.UsuarioId,
                ProductoNombreSnapshot = "Removido",
                GrupoNombre = "General",
                Orden = 1,
                Cantidad = 1,
                Unidad = "u",
                CreatedAt = DateTime.UtcNow,
                Comprado = false,
                AgregadoAlInventario = false,
                RemovidoDeListaAt = DateTime.UtcNow
            });
        await _db.SaveChangesAsync();

        var result = await _readService.GetPendingShoppingItemsAsync(link.HogarId, limit: 12, CancellationToken.None);

        var only = Assert.Single(result.Items);
        Assert.Equal("Pendiente", only.Name);
    }

    [Fact]
    public async Task GetPendingShoppingItemsAsync_ResolvesManualGroupToNullGroupName()
    {
        // The read service collapses the manual "Productos agregados"
        // group to a null GroupName so the provider does not emit a header
        // for it. This is the read service's responsibility; the provider
        // unit tests rely on this contract.
        var link = await SeedLinkedUserAsync();
        var list = new ListaCompraHogar
        {
            Id = Guid.NewGuid(),
            HogarId = link.HogarId,
            Nombre = "Principal",
            CreadaPor = link.UsuarioId,
            CreatedAt = DateTime.UtcNow
        };
        _db.ListasCompraHogar.Add(list);

        _db.ListaCompras.AddRange(
            new ListaCompra
            {
                Id = Guid.NewGuid(),
                HogarId = link.HogarId,
                ListaId = list.Id,
                AgregadoPor = link.UsuarioId,
                ProductoNombreSnapshot = "Manual",
                GrupoNombre = ListaComprasDefaults.ManualGroupName,
                Orden = 0,
                Cantidad = 1,
                Unidad = "u",
                CreatedAt = DateTime.UtcNow,
                Comprado = false,
                AgregadoAlInventario = false
            },
            new ListaCompra
            {
                Id = Guid.NewGuid(),
                HogarId = link.HogarId,
                ListaId = list.Id,
                AgregadoPor = link.UsuarioId,
                ProductoNombreSnapshot = "Con grupo",
                GrupoNombre = "Cena",
                Orden = 1,
                Cantidad = 1,
                Unidad = "u",
                CreatedAt = DateTime.UtcNow,
                Comprado = false,
                AgregadoAlInventario = false
            });
        await _db.SaveChangesAsync();

        var result = await _readService.GetPendingShoppingItemsAsync(link.HogarId, limit: 12, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        var manual = Assert.Single(result.Items, item => item.Name == "Manual");
        Assert.Null(manual.GroupName);
        var grouped = Assert.Single(result.Items, item => item.Name == "Con grupo");
        Assert.Equal("Cena", grouped.GroupName);
    }

    [Fact]
    public async Task GetPendingShoppingItemsAsync_IsolatesByHousehold()
    {
        var link = await SeedLinkedUserAsync();
        var otherHogar = new Hogare { Id = Guid.NewGuid(), Nombre = "Otro hogar", CreatedAt = DateTime.UtcNow };
        _db.Hogares.Add(otherHogar);

        var list = new ListaCompraHogar
        {
            Id = Guid.NewGuid(),
            HogarId = link.HogarId,
            Nombre = "Principal",
            CreadaPor = link.UsuarioId,
            CreatedAt = DateTime.UtcNow
        };
        _db.ListasCompraHogar.Add(list);

        _db.ListaCompras.AddRange(
            new ListaCompra
            {
                Id = Guid.NewGuid(),
                HogarId = link.HogarId,
                ListaId = list.Id,
                AgregadoPor = link.UsuarioId,
                ProductoNombreSnapshot = "Mio",
                GrupoNombre = "General",
                Orden = 0,
                Cantidad = 1,
                Unidad = "u",
                CreatedAt = DateTime.UtcNow,
                Comprado = false,
                AgregadoAlInventario = false
            },
            new ListaCompra
            {
                Id = Guid.NewGuid(),
                HogarId = otherHogar.Id,
                AgregadoPor = link.UsuarioId,
                ProductoNombreSnapshot = "Ajeno",
                GrupoNombre = "General",
                Orden = 0,
                Cantidad = 1,
                Unidad = "u",
                CreatedAt = DateTime.UtcNow,
                Comprado = false,
                AgregadoAlInventario = false
            });
        await _db.SaveChangesAsync();

        var result = await _readService.GetPendingShoppingItemsAsync(link.HogarId, limit: 12, CancellationToken.None);

        var only = Assert.Single(result.Items);
        Assert.Equal("Mio", only.Name);
    }

    [Fact]
    public async Task GetPendingAssignedTasksAsync_WhenNoData_ReturnsEmptyResultWithoutOverflow()
    {
        var link = await SeedLinkedUserAsync();

        var result = await _readService.GetPendingAssignedTasksAsync(link.HogarId, link.UsuarioId, limit: 8, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.False(result.HasMore);
        Assert.Equal(0, result.RemainingCount);
    }

    [Fact]
    public async Task GetPendingAssignedTasksAsync_IncludesOnlyNonCompletedTasksAssignedToLinkedUser()
    {
        var link = await SeedLinkedUserAsync();
        var linkedUser = await _db.Usuarios.FindAsync(link.UsuarioId) ?? throw new InvalidOperationException();
        var otherUser = CreateUser("Otro usuario");
        var creator = CreateUser("Creador");

        _db.Usuarios.AddRange(otherUser, creator);

        var assignedPending = new Tarea
        {
            Id = Guid.NewGuid(),
            HogarId = link.HogarId,
            CreadoPor = creator.Id,
            CreadoPorNavigation = creator,
            Titulo = "Sacar la basura",
            Estado = "pendiente",
            FechaLimite = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        };
        var completedForLinked = new Tarea
        {
            Id = Guid.NewGuid(),
            HogarId = link.HogarId,
            CreadoPor = creator.Id,
            CreadoPorNavigation = creator,
            Titulo = "Lavar platos",
            Estado = "completada",
            CreatedAt = DateTime.UtcNow
        };
        var pendingForOtherUser = new Tarea
        {
            Id = Guid.NewGuid(),
            HogarId = link.HogarId,
            CreadoPor = creator.Id,
            CreadoPorNavigation = creator,
            Titulo = "Ordenar alacena",
            Estado = "en_progreso",
            CreatedAt = DateTime.UtcNow
        };

        _db.Tareas.AddRange(assignedPending, completedForLinked, pendingForOtherUser);
        _db.AsignacionesTareas.AddRange(
            new AsignacionesTarea { Id = Guid.NewGuid(), TareaId = assignedPending.Id, Tarea = assignedPending, UsuarioId = link.UsuarioId, Usuario = linkedUser, FechaAsignacion = DateTime.UtcNow },
            new AsignacionesTarea { Id = Guid.NewGuid(), TareaId = completedForLinked.Id, Tarea = completedForLinked, UsuarioId = link.UsuarioId, Usuario = linkedUser, FechaAsignacion = DateTime.UtcNow },
            new AsignacionesTarea { Id = Guid.NewGuid(), TareaId = pendingForOtherUser.Id, Tarea = pendingForOtherUser, UsuarioId = otherUser.Id, Usuario = otherUser, FechaAsignacion = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var result = await _readService.GetPendingAssignedTasksAsync(link.HogarId, link.UsuarioId, limit: 8, CancellationToken.None);

        var only = Assert.Single(result.Items);
        Assert.Equal("Sacar la basura", only.Title);
        Assert.Equal("pendiente", only.Status);
        Assert.NotNull(only.DueDate);
        Assert.Equal(assignedPending.Id, only.TaskId);
        // Tasks that are not assigned or are completed must not expose their id.
        Assert.DoesNotContain(result.Items, item => item.TaskId == completedForLinked.Id);
        Assert.DoesNotContain(result.Items, item => item.TaskId == pendingForOtherUser.Id);
    }

    [Fact]
    public async Task GetPendingAssignedTasksAsync_RespectsLimitAndReportsOverflow()
    {
        var link = await SeedLinkedUserAsync();
        var linkedUser = await _db.Usuarios.FindAsync(link.UsuarioId) ?? throw new InvalidOperationException();
        var creator = CreateUser("Creador");
        _db.Usuarios.Add(creator);

        for (var i = 0; i < 4; i++)
        {
            var task = new Tarea
            {
                Id = Guid.NewGuid(),
                HogarId = link.HogarId,
                CreadoPor = creator.Id,
                CreadoPorNavigation = creator,
                Titulo = $"Tarea {i}",
                Estado = "pendiente",
                CreatedAt = DateTime.UtcNow
            };
            _db.Tareas.Add(task);
            _db.AsignacionesTareas.Add(new AsignacionesTarea
            {
                Id = Guid.NewGuid(),
                TareaId = task.Id,
                Tarea = task,
                UsuarioId = link.UsuarioId,
                Usuario = linkedUser,
                FechaAsignacion = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        var result = await _readService.GetPendingAssignedTasksAsync(link.HogarId, link.UsuarioId, limit: 2, CancellationToken.None);

        // `limit + 1` overflow probe: at least one more row exists; the
        // exact total would require a separate COUNT we deliberately avoid.
        Assert.Equal(2, result.Items.Count);
        Assert.True(result.HasMore);
        Assert.True(result.RemainingCount >= 1);
    }

    [Fact]
    public async Task GetPendingAssignedTasksAsync_IsolatesByHousehold()
    {
        var link = await SeedLinkedUserAsync();
        var otherHogar = new Hogare { Id = Guid.NewGuid(), Nombre = "Otro hogar", CreatedAt = DateTime.UtcNow };
        _db.Hogares.Add(otherHogar);

        var linkedUser = await _db.Usuarios.FindAsync(link.UsuarioId) ?? throw new InvalidOperationException();
        var creator = CreateUser("Creador");
        _db.Usuarios.Add(creator);

        var ownTask = new Tarea
        {
            Id = Guid.NewGuid(),
            HogarId = link.HogarId,
            CreadoPor = creator.Id,
            CreadoPorNavigation = creator,
            Titulo = "Mia",
            Estado = "pendiente",
            CreatedAt = DateTime.UtcNow
        };
        var otherTask = new Tarea
        {
            Id = Guid.NewGuid(),
            HogarId = otherHogar.Id,
            CreadoPor = creator.Id,
            CreadoPorNavigation = creator,
            Titulo = "Ajena",
            Estado = "pendiente",
            CreatedAt = DateTime.UtcNow
        };

        _db.Tareas.AddRange(ownTask, otherTask);
        _db.AsignacionesTareas.AddRange(
            new AsignacionesTarea { Id = Guid.NewGuid(), TareaId = ownTask.Id, Tarea = ownTask, UsuarioId = link.UsuarioId, Usuario = linkedUser, FechaAsignacion = DateTime.UtcNow },
            new AsignacionesTarea { Id = Guid.NewGuid(), TareaId = otherTask.Id, Tarea = otherTask, UsuarioId = link.UsuarioId, Usuario = linkedUser, FechaAsignacion = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var result = await _readService.GetPendingAssignedTasksAsync(link.HogarId, link.UsuarioId, limit: 8, CancellationToken.None);

        var only = Assert.Single(result.Items);
        Assert.Equal("Mia", only.Title);
    }

    private async Task<TelegramChatLinkSnapshot> SeedLinkedUserAsync()
    {
        var hogar = new Hogare
        {
            Id = Guid.NewGuid(),
            Nombre = "Hogar Telegram",
            CreatedAt = DateTime.UtcNow
        };
        var user = CreateUser("Usuario Telegram");

        _db.Hogares.Add(hogar);
        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        return new TelegramChatLinkSnapshot(10, user.Id, hogar.Id, DateTime.UtcNow, null);
    }

    private static Usuario CreateUser(string name)
        => new()
        {
            Id = Guid.NewGuid(),
            Nombre = name,
            Email = $"{Guid.NewGuid():N}@test.local",
            Sexo = "U",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private Nido.Infrastructure.Persistence.Entities.StockHogar CreateStock(TelegramChatLinkSnapshot link, Producto product, decimal quantity, string unit, int containers, DateOnly? dueDate = null)
        => CreateStockForHogar(link.HogarId, link.UsuarioId, product, quantity, unit, containers, dueDate);

    private Nido.Infrastructure.Persistence.Entities.StockHogar CreateStockForHogar(Guid hogarId, Guid usuarioId, Producto product, decimal quantity, string unit, int containers, DateOnly? dueDate = null)
        => new()
        {
            Id = Guid.NewGuid(),
            HogarId = hogarId,
            ProductoId = product.Id,
            Producto = product,
            CargadoPor = usuarioId,
            UpdatedBy = usuarioId,
            CantidadActual = quantity,
            UnidadMedida = unit,
            CantidadEnvases = containers,
            FechaVencimiento = dueDate,
            Ubicacion = "Alacena",
            CreatedAt = DateTime.UtcNow
        };
}
