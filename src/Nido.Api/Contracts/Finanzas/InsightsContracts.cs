namespace Nido.Api.Contracts.Finanzas;

public sealed record InsightResponse(
    string Tipo,
    string Categoria,
    string Titulo,
    string Detalle,
    string? Accion,
    string Emoji,
    int VariacionPct,
    int TendenciaMeses
);

public sealed record InsightsListResponse(IReadOnlyList<InsightResponse> Insights);
