namespace Nido.Application.Finanzas;

public sealed record InsightResult(
    string Tipo,
    string Categoria,
    string Titulo,
    string Detalle,
    string? Accion,
    string Emoji,
    int VariacionPct,
    int TendenciaMeses
);

public sealed record InsightsListResult(IReadOnlyList<InsightResult> Insights);
