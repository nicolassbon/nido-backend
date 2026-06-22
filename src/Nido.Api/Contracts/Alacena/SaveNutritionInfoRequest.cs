namespace Nido.Api.Contracts.Alacena;

public sealed record SaveNutritionInfoRequest(
    decimal? Calorias,
    decimal? Proteinas,
    decimal? Carbohidratos,
    decimal? Grasas,
    string? Porcion,
    string? Base,
    IReadOnlyList<SaveNutritionInfoItemRequest>? Items);

public sealed record SaveNutritionInfoItemRequest(
    string Nombre,
    decimal? Valor,
    string? Unidad,
    decimal? PorcentajeDiario,
    int Orden);

