namespace Nido.Application.Recetas;

public sealed record RecetaResult(
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
    IReadOnlyList<RecetaElectrodomesticoResult> Electrodomesticos);

public sealed record RecetaIngredienteResult(
    Guid Id,
    Guid? ProductoId,
    string Nombre,
    string? ProductoNombre,
    decimal? Cantidad,
    string? Unidad);

public sealed record RecetaPasoResult(
    Guid Id,
    int Orden,
    string Descripcion);

public sealed record RecetaElectrodomesticoResult(
    Guid Id,
    string? TipoRequerido);
