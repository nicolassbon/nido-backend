namespace Nido.Application.Alacena;

public sealed record NutritionInfoResult(
    decimal? Calorias,
    decimal? Proteinas,
    decimal? Carbohidratos,
    decimal? Grasas,
    string? Porcion,
    string? Base,
    IReadOnlyList<NutritionInfoItemResult> Items);

public sealed record NutritionInfoItemResult(
    string Nombre,
    decimal? Valor,
    string? Unidad,
    decimal? PorcentajeDiario,
    int Orden);

