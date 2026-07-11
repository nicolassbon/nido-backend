namespace Nido.Application.Insights;

public sealed record GetInsightsHogarResult(
    IReadOnlyList<ComprarProntoItem> ComprarPronto,
    IReadOnlyList<PorVencerItem> PorVencer,
    IReadOnlyList<DesperdicioItem> Desperdicios,
    IReadOnlyList<EnvaseZombieItem> EnvasesZombies,
    ClasificacionProductos Clasificacion,
    PatronesCocina Patrones,
    ResumenInsights Resumen);

public sealed record ComprarProntoItem(
    Guid StockHogarId,
    string ProductoNombre,
    decimal StockActual,
    string? UnidadMedida,
    int DiasParaAgotar,
    int FrecuenciaCompraDias,
    string Categoria);

public sealed record SugerenciaNidoItem(
    Guid StockHogarId,
    Guid ProductoId,
    string ProductoNombre,
    decimal StockActual,
    string? UnidadMedida,
    string Icono,
    double Score);

public sealed record PorVencerItem(
    Guid StockHogarId,
    string ProductoNombre,
    string? Imagen,
    decimal Cantidad,
    string? UnidadMedida,
    string FechaVencimiento,
    int DiasParaVencer);

public sealed record DesperdicioItem(
    string ProductoNombre,
    int VecesVencido,
    int VecesCocinado,
    decimal TasaDesperdicioPorc,
    string Sugerencia);

public sealed record EnvaseZombieItem(
    Guid StockHogarId,
    string ProductoNombre,
    int DiasAbierto,
    string Sugerencia);

public sealed record ClasificacionProductos(
    IReadOnlyList<ProductoClasificado> Esenciales,
    IReadOnlyList<ProductoClasificado> Recurrentes,
    IReadOnlyList<ProductoClasificado> Ocasionales,
    IReadOnlyList<ProductoClasificado> Caprichosos);

public sealed record ProductoClasificado(
    string ProductoNombre,
    int VecesComprado,
    int VecesVencido,
    int FrecuenciaCompraDias);

public sealed record PatronesCocina(
    string? DiaQueMasCocinan,
    int CocinadasEnDiaTop,
    IReadOnlyList<string> IngredientesTop,
    IReadOnlyList<RecetaTopItem> RecetasTop);

public sealed record RecetaTopItem(
    Guid RecetaId,
    string Nombre,
    int VecesCocinada);

public sealed record ResumenInsights(
    int TotalProductosAlacena,
    int ProductosPorVencerSemana,
    int ConsumosUltimos30Dias,
    decimal TasaDesperdicioPorc,
    decimal TasaDesperdicioMesAnteriorPorc,
    int FrecuenciaCompraHogarDias,
    int ProductosEsenciales);
