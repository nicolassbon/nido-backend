namespace Nido.Api.Contracts.Alacena;

public sealed record NutritionInfoResponse(
    decimal? Calorias,
    decimal? Proteinas,
    decimal? Carbohidratos,
    decimal? Grasas,
    string? Porcion,
    string? Base,
    IReadOnlyList<NutritionInfoItemResponse> Items);

public sealed record NutritionInfoItemResponse(
    string Nombre,
    decimal? Valor,
    string? Unidad,
    decimal? PorcentajeDiario,
    int Orden);

