namespace Nido.Api.Contracts.Recetas;

public sealed record RecetaResponse(
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
    IReadOnlyList<RecetaIngredienteResponse> Ingredientes,
    IReadOnlyList<RecetaPasoResponse> Pasos,
    IReadOnlyList<RecetaElectrodomesticoResponse> Electrodomesticos);

public sealed record RecetaIngredienteResponse(
    Guid Id,
    Guid? ProductoId,
    string Nombre,
    string? ProductoNombre,
    decimal? Cantidad,
    string? Unidad);

public sealed record RecetaPasoResponse(
    Guid Id,
    int Orden,
    string Descripcion);

public sealed record RecetaElectrodomesticoResponse(
    Guid Id,
    string? TipoRequerido);
