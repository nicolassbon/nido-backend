namespace Nido.Application.Productos;

public sealed record GetProductByBarcodeQuery(string CodigoBarras, Guid? HogarId = null);
