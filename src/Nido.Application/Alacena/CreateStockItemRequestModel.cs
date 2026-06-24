namespace Nido.Application.Alacena;

public sealed record CreateStockItemRequestModel(
    Guid HogarId,
    Guid UsuarioId,
    string Nombre,
    Guid? CategoriaId,
    string? CodigoBarras,
    string? Imagen,
    string Ubicacion,
    decimal Cantidad,
    string? UnidadMedida,
    string? FechaVencimiento,
    bool EstaAbierto,
    decimal PorcentajeConsumido,
    int CantidadEnvases = 1,
    string OrigenCarga = StockLoadOrigins.Manual,
    decimal? Calorias = null,
    decimal? Proteinas = null,
    decimal? Carbohidratos = null,
    decimal? Grasas = null,
    SaveNutritionInfoRequestModel? InformacionNutricional = null
);
