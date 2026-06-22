namespace Nido.Application.Alacena;

public sealed record SaveNutritionInfoRequestModel(
    decimal? Calorias,
    decimal? Proteinas,
    decimal? Carbohidratos,
    decimal? Grasas,
    string? Porcion,
    string? Base,
    IReadOnlyList<SaveNutritionInfoItemRequestModel> Items);

public sealed record SaveNutritionInfoItemRequestModel(
    string Nombre,
    decimal? Valor,
    string? Unidad,
    decimal? PorcentajeDiario,
    int Orden);

