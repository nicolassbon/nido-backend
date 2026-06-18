using Nido.Application.Alacena;
using Nido.Application.Finanzas;
using Nido.Application.Finanzas.Exceptions;
using Nido.Application.Recetas;

namespace Nido.Application.Tests.Finanzas;

public sealed class FinanzasHandlersTests
{
    // ─── CreateGastoHandler ──────────────────────────────────────────────────

    [Fact(DisplayName = "CreateGasto: monto cero lanza InvalidGastoMontoException")]
    public async Task CreateGasto_WithZeroMonto_ThrowsInvalidGastoMonto()
    {
        var handler = new CreateGastoHandler(new FakeFinanzasRepository());

        await Assert.ThrowsAsync<InvalidGastoMontoException>(() =>
            handler.Handle(
                new CreateGastoCommand(Guid.NewGuid(), Guid.NewGuid(), 0, null, null, "2026-01-15"),
                CancellationToken.None));
    }

    [Fact(DisplayName = "CreateGasto: monto negativo lanza InvalidGastoMontoException")]
    public async Task CreateGasto_WithNegativeMonto_ThrowsInvalidGastoMonto()
    {
        var handler = new CreateGastoHandler(new FakeFinanzasRepository());

        await Assert.ThrowsAsync<InvalidGastoMontoException>(() =>
            handler.Handle(
                new CreateGastoCommand(Guid.NewGuid(), Guid.NewGuid(), -100, null, null, "2026-01-15"),
                CancellationToken.None));
    }

    [Fact(DisplayName = "CreateGasto: fecha inválida lanza InvalidGastoFechaException")]
    public async Task CreateGasto_WithInvalidFecha_ThrowsInvalidGastoFecha()
    {
        var handler = new CreateGastoHandler(new FakeFinanzasRepository());

        await Assert.ThrowsAsync<InvalidGastoFechaException>(() =>
            handler.Handle(
                new CreateGastoCommand(Guid.NewGuid(), Guid.NewGuid(), 100, null, null, "no-es-fecha"),
                CancellationToken.None));
    }

    [Fact(DisplayName = "CreateGasto: categoría inválida lanza InvalidGastoCategoriaException")]
    public async Task CreateGasto_WithInvalidCategoria_ThrowsInvalidGastoCategoria()
    {
        var handler = new CreateGastoHandler(new FakeFinanzasRepository());

        await Assert.ThrowsAsync<InvalidGastoCategoriaException>(() =>
            handler.Handle(
                new CreateGastoCommand(Guid.NewGuid(), Guid.NewGuid(), 100, null, "Entretenimiento", "2026-01-15"),
                CancellationToken.None));
    }

    [Fact(DisplayName = "CreateGasto: datos válidos devuelve el gasto del repositorio")]
    public async Task CreateGasto_WithValidData_ReturnsGastoFromRepository()
    {
        var handler = new CreateGastoHandler(new FakeFinanzasRepository());

        var result = await handler.Handle(
            new CreateGastoCommand(Guid.NewGuid(), Guid.NewGuid(), 500, "Supermercado", "Comida", "2026-01-15"),
            CancellationToken.None);

        Assert.Equal(500, result.Monto);
        Assert.Equal("Comida", result.Categoria);
    }

    [Fact(DisplayName = "CreateGasto: categoría null se acepta sin error")]
    public async Task CreateGasto_WithNullCategoria_Succeeds()
    {
        var handler = new CreateGastoHandler(new FakeFinanzasRepository());

        var result = await handler.Handle(
            new CreateGastoCommand(Guid.NewGuid(), Guid.NewGuid(), 200, null, null, "2026-01-15"),
            CancellationToken.None);

        Assert.Equal(200, result.Monto);
    }

    // ─── GetBalanceHandler ───────────────────────────────────────────────────

    [Fact(DisplayName = "GetBalance: 'desde' inválido lanza InvalidGastoFechaException")]
    public async Task GetBalance_WithInvalidDesde_ThrowsInvalidGastoFecha()
    {
        var handler = new GetBalanceHandler(new FakeFinanzasRepository());

        await Assert.ThrowsAsync<InvalidGastoFechaException>(() =>
            handler.Handle(
                new GetBalanceQuery(Guid.NewGuid(), "mala-fecha", null),
                CancellationToken.None));
    }

    [Fact(DisplayName = "GetBalance: 'hasta' inválido lanza InvalidGastoFechaException")]
    public async Task GetBalance_WithInvalidHasta_ThrowsInvalidGastoFecha()
    {
        var handler = new GetBalanceHandler(new FakeFinanzasRepository());

        await Assert.ThrowsAsync<InvalidGastoFechaException>(() =>
            handler.Handle(
                new GetBalanceQuery(Guid.NewGuid(), "2026-01-01", "mala-fecha"),
                CancellationToken.None));
    }

    [Fact(DisplayName = "GetBalance: fechas válidas devuelven resultado del repositorio")]
    public async Task GetBalance_WithValidDates_ReturnsResult()
    {
        var handler = new GetBalanceHandler(new FakeFinanzasRepository());

        var result = await handler.Handle(
            new GetBalanceQuery(Guid.NewGuid(), "2026-01-01", "2026-01-31"),
            CancellationToken.None);

        Assert.NotNull(result);
    }

    // ─── GetSavingsPotencialHandler ──────────────────────────────────────────

    [Fact(DisplayName = "GetSavingsPotencial: sin historial devuelve lista vacía y potencial cero")]
    public async Task GetSavingsPotencial_WithNoHistoricData_ReturnsEmpty()
    {
        var repo = new FakeFinanzasRepository();
        repo.EnqueueGastos(new GetGastosResult([], 0)); // mes actual
        repo.EnqueueGastos(new GetGastosResult([], 0)); // histórico vacío
        var handler = new GetSavingsPotencialHandler(repo);

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(result.Categorias);
        Assert.Equal(0, result.TotalPotencial);
    }

    [Fact(DisplayName = "GetSavingsPotencial: variación mayor al 10% incluye la categoría")]
    public async Task GetSavingsPotencial_WhenCurrentExceedsHistoricByMoreThan10Pct_IncludesCategory()
    {
        var repo = new FakeFinanzasRepository();
        // Mes actual: 1200 en Comida
        repo.EnqueueGastos(new GetGastosResult(
            [MakeGasto(1200, "Comida", "2026-01-15")], 1200));
        // Histórico 3 meses distintos: 1000 cada uno → promedio 1000
        repo.EnqueueGastos(new GetGastosResult(
        [
            MakeGasto(1000, "Comida", "2025-10-15"),
            MakeGasto(1000, "Comida", "2025-11-15"),
            MakeGasto(1000, "Comida", "2025-12-15"),
        ], 3000));
        var handler = new GetSavingsPotencialHandler(repo);

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        Assert.Single(result.Categorias);
        Assert.Equal("Comida", result.Categorias[0].Nombre);
        Assert.True(result.TotalPotencial > 0);
    }

    [Fact(DisplayName = "GetSavingsPotencial: variación del 10% o menos excluye la categoría")]
    public async Task GetSavingsPotencial_WhenVariacionIs10PctOrLess_ExcludesCategory()
    {
        var repo = new FakeFinanzasRepository();
        // Variación del 5%: 1050 vs promedio 1000
        repo.EnqueueGastos(new GetGastosResult(
            [MakeGasto(1050, "Comida", "2026-01-15")], 1050));
        repo.EnqueueGastos(new GetGastosResult(
        [
            MakeGasto(1000, "Comida", "2025-10-15"),
            MakeGasto(1000, "Comida", "2025-11-15"),
            MakeGasto(1000, "Comida", "2025-12-15"),
        ], 3000));
        var handler = new GetSavingsPotencialHandler(repo);

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(result.Categorias);
    }

    [Fact(DisplayName = "GetSavingsPotencial: las categorías se ordenan por potencial de ahorro descendente")]
    public async Task GetSavingsPotencial_CategoriesAreSortedByPotencialDescending()
    {
        var repo = new FakeFinanzasRepository();
        // Comida: potencial 1000; Transporte: potencial 200
        repo.EnqueueGastos(new GetGastosResult(
        [
            MakeGasto(2000, "Comida",     "2026-01-15"),
            MakeGasto(600,  "Transporte", "2026-01-15"),
        ], 2600));
        repo.EnqueueGastos(new GetGastosResult(
        [
            MakeGasto(1000, "Comida",     "2025-10-15"),
            MakeGasto(400,  "Transporte", "2025-10-15"),
            MakeGasto(1000, "Comida",     "2025-11-15"),
            MakeGasto(400,  "Transporte", "2025-11-15"),
            MakeGasto(1000, "Comida",     "2025-12-15"),
            MakeGasto(400,  "Transporte", "2025-12-15"),
        ], 4200));
        var handler = new GetSavingsPotencialHandler(repo);

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(2, result.Categorias.Count);
        Assert.True(result.Categorias[0].PotencialAhorro >= result.Categorias[1].PotencialAhorro);
        Assert.Equal("Comida", result.Categorias[0].Nombre);
    }

    // ─── GetInsightsHandler ──────────────────────────────────────────────────

    [Fact(DisplayName = "GetInsights: aumento del 20% o más genera una alerta")]
    public async Task GetInsights_WhenIncreaseIs20PctOrMore_GeneratesAlerta()
    {
        var repo = new FakeFinanzasRepository();
        // Actual: 1200 (+20% vs mes -1: 1000)
        repo.EnqueueGastos(GastosParaMes(1200, "Comida"));
        repo.EnqueueGastos(GastosParaMes(1000, "Comida")); // mes -1
        repo.EnqueueGastos(new GetGastosResult([], 0));    // mes -2
        repo.EnqueueGastos(new GetGastosResult([], 0));    // mes -3
        var handler = new GetInsightsHandler(repo);

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        var alerta = result.Insights.FirstOrDefault(i => i.Tipo == "alerta");
        Assert.NotNull(alerta);
        Assert.Equal("Comida", alerta.Categoria);
    }

    [Fact(DisplayName = "GetInsights: bajada del 20% o más genera un positivo con emoji de festejo")]
    public async Task GetInsights_WhenDecreaseIs20PctOrMore_GeneratesPositivo()
    {
        var repo = new FakeFinanzasRepository();
        // Actual: 700 (-30% vs mes -1: 1000)
        repo.EnqueueGastos(GastosParaMes(700, "Transporte"));
        repo.EnqueueGastos(GastosParaMes(1000, "Transporte")); // mes -1
        repo.EnqueueGastos(new GetGastosResult([], 0));
        repo.EnqueueGastos(new GetGastosResult([], 0));
        var handler = new GetInsightsHandler(repo);

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        var positivo = result.Insights.FirstOrDefault(i => i.Tipo == "positivo");
        Assert.NotNull(positivo);
        Assert.Equal("🎉", positivo.Emoji);
    }

    [Fact(DisplayName = "GetInsights: variación menor al 10% genera un positivo de categoría estable")]
    public async Task GetInsights_WhenChangeIsLessThan10Pct_GeneratesEstable()
    {
        var repo = new FakeFinanzasRepository();
        // Variación del 5%: estable
        repo.EnqueueGastos(GastosParaMes(1050, "Servicios"));
        repo.EnqueueGastos(GastosParaMes(1000, "Servicios")); // mes -1
        repo.EnqueueGastos(new GetGastosResult([], 0));
        repo.EnqueueGastos(new GetGastosResult([], 0));
        var handler = new GetInsightsHandler(repo);

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        var positivo = result.Insights.FirstOrDefault(i => i.Tipo == "positivo");
        Assert.NotNull(positivo);
        Assert.Contains("estable", positivo.Titulo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "GetInsights: categoría ausente en el mes anterior no genera insight")]
    public async Task GetInsights_WhenCategoryMissingInPreviousMonth_GeneratesNoInsight()
    {
        var repo = new FakeFinanzasRepository();
        // Actual tiene Comida, mes -1 tiene Transporte → sin match
        repo.EnqueueGastos(GastosParaMes(1000, "Comida"));
        repo.EnqueueGastos(GastosParaMes(500, "Transporte")); // mes -1 sin Comida
        repo.EnqueueGastos(new GetGastosResult([], 0));
        repo.EnqueueGastos(new GetGastosResult([], 0));
        var handler = new GetInsightsHandler(repo);

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(result.Insights);
    }

    [Fact(DisplayName = "GetInsights: crecimiento por 3 meses consecutivos genera una tendencia")]
    public async Task GetInsights_WhenGrowthAcrossThreeMonths_GeneratesTendencia()
    {
        var repo = new FakeFinanzasRepository();
        // Mes-3: 700, Mes-2: 800, Mes-1: 900, Actual: 1000 → crecimiento consecutivo, pct ~11%
        repo.EnqueueGastos(GastosParaMes(1000, "Otros")); // actual
        repo.EnqueueGastos(GastosParaMes(900,  "Otros")); // mes -1
        repo.EnqueueGastos(GastosParaMes(800,  "Otros")); // mes -2
        repo.EnqueueGastos(GastosParaMes(700,  "Otros")); // mes -3
        var handler = new GetInsightsHandler(repo);

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        var tendencia = result.Insights.FirstOrDefault(i => i.Tipo == "tendencia");
        Assert.NotNull(tendencia);
        Assert.Equal("Otros", tendencia.Categoria);
    }

    [Fact(DisplayName = "GetInsights: devuelve como máximo 3 insights aunque haya más categorías")]
    public async Task GetInsights_ReturnsAtMostThreeInsights()
    {
        var repo = new FakeFinanzasRepository();
        // 4 categorías con alerta del 30%
        var categorias = new[] { "Comida", "Transporte", "Servicios", "Otros" };
        repo.EnqueueGastos(new GetGastosResult(
            categorias.Select(c => MakeGasto(1300, c, "2026-01-15")).ToList(), 5200));
        repo.EnqueueGastos(new GetGastosResult(
            categorias.Select(c => MakeGasto(1000, c, "2025-12-15")).ToList(), 4000));
        repo.EnqueueGastos(new GetGastosResult([], 0));
        repo.EnqueueGastos(new GetGastosResult([], 0));
        var handler = new GetInsightsHandler(repo);

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.Insights.Count <= 3);
    }

    // ─── GetPresupuestoHandler ───────────────────────────────────────────────

    [Fact(DisplayName = "GetPresupuesto: sin presupuesto configurado devuelve Monto y Restante null")]
    public async Task GetPresupuesto_WithNoPresupuesto_ReturnsNullMontoAndRestante()
    {
        var repo = new FakeFinanzasRepository();
        repo.EnqueueGastos(new GetGastosResult([], 0));
        repo.PresupuestoMonto = null;
        var handler = new GetPresupuestoHandler(repo);

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result.Monto);
        Assert.Null(result.Restante);
        Assert.Equal(0, result.GastoActual);
    }

    [Fact(DisplayName = "GetPresupuesto: con presupuesto y gastos calcula el restante correctamente")]
    public async Task GetPresupuesto_WithPresupuestoAndGastos_CalculatesRestante()
    {
        var repo = new FakeFinanzasRepository();
        repo.EnqueueGastos(new GetGastosResult([MakeGasto(300, "Comida", "2026-06-10")], 300));
        repo.PresupuestoMonto = 1000;
        var handler = new GetPresupuestoHandler(repo);

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(1000, result.Monto);
        Assert.Equal(300, result.GastoActual);
        Assert.Equal(700, result.Restante);
    }

    // ─── SetPresupuestoHandler ───────────────────────────────────────────────

    [Fact(DisplayName = "SetPresupuesto: guarda el monto y devuelve el restante calculado")]
    public async Task SetPresupuesto_SavesMontoAndReturnsRestante()
    {
        var repo = new FakeFinanzasRepository();
        repo.UpsertedPresupuesto = 2000;
        repo.EnqueueGastos(new GetGastosResult([MakeGasto(500, "Otros", "2026-06-01")], 500));
        var handler = new SetPresupuestoHandler(repo);

        var result = await handler.Handle(
            new SetPresupuestoCommand(Guid.NewGuid(), 2000, 2026, 6),
            CancellationToken.None);

        Assert.Equal(2000, result.Monto);
        Assert.Equal(500, result.GastoActual);
        Assert.Equal(1500, result.Restante);
    }

    // ─── UpdateGastoHandler ──────────────────────────────────────────────────

    [Fact(DisplayName = "UpdateGasto: gasto existente devuelve el gasto actualizado")]
    public async Task UpdateGasto_ExistingGasto_ReturnsUpdatedGasto()
    {
        var updated = MakeGasto(800, "Transporte", "2026-06-15");
        var repo = new FakeFinanzasRepository { GastoToUpdate = updated };
        var handler = new UpdateGastoHandler(repo);

        var result = await handler.Handle(
            new UpdateGastoCommand(Guid.NewGuid(), Guid.NewGuid(), 800, null, "Transporte", "2026-06-15", Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(800, result.Monto);
        Assert.Equal("Transporte", result.Categoria);
    }

    [Fact(DisplayName = "UpdateGasto: gasto no encontrado lanza GastoNotFoundException")]
    public async Task UpdateGasto_NotFound_ThrowsGastoNotFoundException()
    {
        var repo = new FakeFinanzasRepository { GastoToUpdate = null };
        var handler = new UpdateGastoHandler(repo);

        await Assert.ThrowsAsync<GastoNotFoundException>(() =>
            handler.Handle(
                new UpdateGastoCommand(Guid.NewGuid(), Guid.NewGuid(), 100, null, null, "2026-06-01", Guid.NewGuid()),
                CancellationToken.None));
    }

    // ─── DeleteGastoHandler ──────────────────────────────────────────────────

    [Fact(DisplayName = "DeleteGasto: gasto sin factura devuelve FacturaRevertida false")]
    public async Task DeleteGasto_WithoutFactura_ReturnsFacturaRevertidaFalse()
    {
        var repo = new FakeFinanzasRepository { DeleteGastoResultValue = new DeleteGastoResult(false, null) };
        var handler = new DeleteGastoHandler(repo);

        var result = await handler.Handle(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.FacturaRevertida);
        Assert.Null(result.FacturaId);
    }

    [Fact(DisplayName = "DeleteGasto: gasto originado en una factura revierte la factura")]
    public async Task DeleteGasto_FromFactura_ReturnsFacturaRevertidaTrue()
    {
        var facturaId = Guid.NewGuid();
        var repo = new FakeFinanzasRepository { DeleteGastoResultValue = new DeleteGastoResult(true, facturaId) };
        var handler = new DeleteGastoHandler(repo);

        var result = await handler.Handle(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.FacturaRevertida);
        Assert.Equal(facturaId, result.FacturaId);
    }

    [Fact(DisplayName = "DeleteGasto: gasto no encontrado lanza GastoNotFoundException")]
    public async Task DeleteGasto_NotFound_ThrowsGastoNotFoundException()
    {
        var repo = new FakeFinanzasRepository { DeleteGastoResultValue = null };
        var handler = new DeleteGastoHandler(repo);

        await Assert.ThrowsAsync<GastoNotFoundException>(() =>
            handler.Handle(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
    }

    // ─── GetAlacenaOportunidadesHandler ──────────────────────────────────────

    [Fact(DisplayName = "GetAlacenaOportunidades: excluye productos con consumo del 90% o más")]
    public async Task GetAlacenaOportunidades_ExcludesItemsWithHighConsumption()
    {
        var alacenaRepo = new FakeAlacenaRepository
        {
            Items =
            [
                MakeStockItem("Leche", porcentajeConsumido: 95), // consumido: excluido
                MakeStockItem("Arroz", porcentajeConsumido: 50), // disponible: incluido
            ]
        };
        var handler = new GetAlacenaOportunidadesHandler(alacenaRepo, new FakeRecetaRepository());

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(1, result.TotalProductosEnCasa);
        Assert.DoesNotContain(result.ProductosDestacados, p => p.Nombre == "Leche");
    }

    [Fact(DisplayName = "GetAlacenaOportunidades: excluye productos vencidos")]
    public async Task GetAlacenaOportunidades_ExcludesExpiredItems()
    {
        var alacenaRepo = new FakeAlacenaRepository
        {
            Items =
            [
                MakeStockItem("Yogur",  porcentajeConsumido: 20, fechaVencimiento: "2020-01-01"), // vencido
                MakeStockItem("Harina", porcentajeConsumido: 30), // sin vencimiento: incluido
            ]
        };
        var handler = new GetAlacenaOportunidadesHandler(alacenaRepo, new FakeRecetaRepository());

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(1, result.TotalProductosEnCasa);
        Assert.DoesNotContain(result.ProductosDestacados, p => p.Nombre == "Yogur");
    }

    [Fact(DisplayName = "GetAlacenaOportunidades: sugiere recetas ordenadas por cobertura de ingredientes en stock")]
    public async Task GetAlacenaOportunidades_SuggestsRecipesSortedByStockCoverage()
    {
        var recetaRepo = new FakeRecetaRepository
        {
            Recetas =
            [
                MakeReceta("Arroz con leche", enStock: 1, total: 3), // 33% cobertura
                MakeReceta("Pasta bolognesa",  enStock: 3, total: 3), // 100% cobertura
            ]
        };
        var handler = new GetAlacenaOportunidadesHandler(new FakeAlacenaRepository(), recetaRepo);

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal("Pasta bolognesa", result.RecetasSugeridas[0].Nombre);
    }

    [Fact(DisplayName = "GetAlacenaOportunidades: excluye recetas sin ningún ingrediente en stock")]
    public async Task GetAlacenaOportunidades_ExcludesRecipesWithNoIngredientsInStock()
    {
        var recetaRepo = new FakeRecetaRepository
        {
            Recetas =
            [
                MakeReceta("Receta sin stock", enStock: 0, total: 3),
            ]
        };
        var handler = new GetAlacenaOportunidadesHandler(new FakeAlacenaRepository(), recetaRepo);

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(result.RecetasSugeridas);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static GastoResult MakeGasto(decimal monto, string categoria, string fecha) =>
        new(Guid.NewGuid(), monto, null, categoria, fecha, Guid.NewGuid(), "Usuario", DateTime.UtcNow, null);

    private static GetGastosResult GastosParaMes(decimal monto, string categoria) =>
        new([MakeGasto(monto, categoria, "2026-01-15")], monto);

    private static StockItemResult MakeStockItem(string nombre, decimal porcentajeConsumido, string? fechaVencimiento = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), nombre, null, null, null, "Alacena", 1, "unidad", fechaVencimiento, false, porcentajeConsumido);

    private static RecetaResult MakeReceta(string nombre, int enStock, int total)
    {
        var ingredientes = Enumerable.Range(0, total)
            .Select(i => new RecetaIngredienteResult(Guid.NewGuid(), null, $"Ingrediente{i}", null, 1, null, i < enStock, []))
            .ToList<RecetaIngredienteResult>();
        return new RecetaResult(Guid.NewGuid(), nombre, null, null, null, null, null, null, null, null, null, null, ingredientes, [], [], 0);
    }

    // ─── Fakes ───────────────────────────────────────────────────────────────

    private sealed class FakeFinanzasRepository : IFinanzasRepository
    {
        private readonly Queue<GetGastosResult> _gastosQueue = new();

        public void EnqueueGastos(GetGastosResult result) => _gastosQueue.Enqueue(result);

        public Task<GetGastosResult> GetGastosAsync(GetGastosQuery query, CancellationToken ct) =>
            Task.FromResult(_gastosQueue.Count > 0 ? _gastosQueue.Dequeue() : new GetGastosResult([], 0));

        public decimal? PresupuestoMonto { get; set; }
        public decimal UpsertedPresupuesto { get; set; }
        public GastoResult? GastoToUpdate { get; set; }
        public DeleteGastoResult? DeleteGastoResultValue { get; set; }

        public Task<GastoResult> CreateGastoAsync(CreateGastoCommand command, CancellationToken ct) =>
            Task.FromResult(new GastoResult(Guid.NewGuid(), command.Monto, command.Descripcion, command.Categoria, command.Fecha, command.PagadoPorId, "Usuario", DateTime.UtcNow, null));

        public Task<GetBalanceResult> GetBalanceAsync(GetBalanceQuery query, CancellationToken ct) =>
            Task.FromResult(new GetBalanceResult([], 0));

        public Task<bool?> GetModoAhorroAsync(Guid hogarId, CancellationToken ct) =>
            Task.FromResult<bool?>(false);

        public Task<bool> ToggleModoAhorroAsync(Guid hogarId, bool activo, CancellationToken ct) =>
            Task.FromResult(activo);

        public Task<FacturaResult> CreateFacturaAsync(CreateFacturaCommand command, Guid facturaId, string? storageKey, CancellationToken ct) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<FacturaResult>> GetFacturasAsync(GetFacturasQuery query, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<FacturaResult>>([]);

        public Task<FacturaResult?> MarcarPagadaAsync(Guid facturaId, Guid hogarId, CancellationToken ct) =>
            Task.FromResult<FacturaResult?>(null);

        public Task<string?> DeleteFacturaAsync(Guid facturaId, Guid hogarId, CancellationToken ct) =>
            Task.FromResult<string?>(null);

        public Task<GastoResult?> UpdateGastoAsync(UpdateGastoCommand command, CancellationToken ct) =>
            Task.FromResult(GastoToUpdate);

        public Task<DeleteGastoResult?> DeleteGastoAsync(Guid gastoId, Guid hogarId, CancellationToken ct) =>
            Task.FromResult(DeleteGastoResultValue);

        public Task<decimal?> GetPresupuestoAsync(Guid hogarId, int anio, int mes, CancellationToken ct) =>
            Task.FromResult(PresupuestoMonto);

        public Task<decimal> UpsertPresupuestoAsync(Guid hogarId, int anio, int mes, decimal monto, CancellationToken ct) =>
            Task.FromResult(UpsertedPresupuesto);
    }

    private sealed class FakeAlacenaRepository : IAlacenaRepository
    {
        public IReadOnlyList<StockItemResult> Items { get; set; } = [];

        public Task<IReadOnlyList<StockItemResult>> GetByHogarAsync(Guid hogarId, CancellationToken ct) =>
            Task.FromResult(Items);

        public Task<StockItemResult?> GetByIdAsync(Guid id, Guid hogarId, CancellationToken ct) =>
            Task.FromResult<StockItemResult?>(null);

        public Task<StockItemResult> CreateAsync(CreateStockItemRequestModel request, CancellationToken ct) =>
            throw new NotImplementedException();

        public Task<StockItemResult?> UpdateAsync(UpdateStockItemRequestModel request, CancellationToken ct) =>
            throw new NotImplementedException();

        public Task<bool> DeleteAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(true);
    }

    private sealed class FakeRecetaRepository : IRecetaRepository
    {
        public IReadOnlyList<RecetaResult> Recetas { get; set; } = [];

        public Task<IReadOnlyList<RecetaResult>> GetAllAsync(Guid hogarId, CancellationToken ct) =>
            Task.FromResult(Recetas);

        public Task<RecetaResult?> GetByIdAsync(Guid id, Guid hogarId, CancellationToken ct) =>
            Task.FromResult<RecetaResult?>(null);

        public Task<CocinarRecetaResult?> CocinarAsync(CocinarRecetaCommand command, CancellationToken ct) =>
            Task.FromResult<CocinarRecetaResult?>(null);
    }
}
