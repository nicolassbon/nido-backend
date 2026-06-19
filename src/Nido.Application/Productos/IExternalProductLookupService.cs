namespace Nido.Application.Productos;

/// <summary>
/// Resuelve un código de barras consultando fuentes externas (Open Food Facts,
/// UPC Item DB, etc.). Siempre resuelve. Devuelve FoundInDb=false sólo cuando
/// ninguna fuente reconoce el código.
/// </summary>
public interface IExternalProductLookupService
{
    Task<LookupExternalProductoResult> LookupAsync(string barcode, CancellationToken ct);
}
