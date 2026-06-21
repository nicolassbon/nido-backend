namespace Nido.Application.Finanzas;

public sealed record AlacenaOportunidadesResult(
    IReadOnlyList<ProductoDestacadoResult> ProductosDestacados,
    IReadOnlyList<RecetaRecomendadaResult> RecetasSugeridas,
    int TotalProductosEnCasa
);

public sealed record ProductoDestacadoResult(
    string Id,
    string Nombre,
    string Ubicacion,
    int? DiasParaVencer,
    string? Imagen,
    int PorcentajeConsumido
);
