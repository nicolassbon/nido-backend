namespace Nido.Application.Recetas;

public sealed record GetRecetaByIdResult(
    Guid Id,
    string Nombre,
    string? Descripcion,
    int? TiempoCoccionMin,
    string? Dificultad,
    int? Porciones,
    string? FuenteId,
    string? ImagenUrl,
    decimal? Calorias,
    decimal? Proteinas,
    decimal? Carbohidratos,
    decimal? Grasas,
    IReadOnlyList<RecetaIngredienteResult> Ingredientes,
    IReadOnlyList<RecetaPasoResult> Pasos,
    IReadOnlyList<RecetaElectrodomesticoResult> Electrodomesticos,
    int VecesCocinada);
