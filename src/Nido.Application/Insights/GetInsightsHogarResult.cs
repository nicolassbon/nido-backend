namespace Nido.Application.Insights;

public sealed record GetInsightsHogarResult(
    IReadOnlyList<ComprarProntoItem> ComprarPronto,
    IReadOnlyList<PorVencerItem> PorVencer,
    IReadOnlyList<DesperdicioItem> Desperdicios,
    ResumenInsights Resumen);

public sealed record ComprarProntoItem(
    string ProductoNombre,
    decimal StockActual,
    string? UnidadMedida,
    double DiasParaAgotar,
    decimal TasaDiariaPromedio);

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
    string Sugerencia);

public sealed record ResumenInsights(
    int TotalProductosAlacena,
    int ProductosPorVencerSemana,
    int ConsumosUltimos30Dias,
    decimal TasaDesperdicioPorc);
