namespace Nido.Api.Contracts.Finanzas;

public sealed record ProductoDestacadoResponse(
    string Id,
    string Nombre,
    string Ubicacion,
    int? DiasParaVencer,
    string? Imagen,
    int PorcentajeConsumido
);

public sealed record AlacenaOportunidadesResponse(
    IReadOnlyList<ProductoDestacadoResponse> ProductosDestacados,
    IReadOnlyList<RecetaRecomendadaResponse> RecetasSugeridas,
    int TotalProductosEnCasa
);
