using System.Text.RegularExpressions;
using Nido.Application.Productos.Exceptions;

namespace Nido.Application.Productos;

public sealed class LookupExternalProductoHandler
{
    /// <summary>
    /// Códigos válidos: 8 a 14 dígitos. Cubre EAN-8, UPC-E, UPC-A, EAN-13,
    /// GTIN-14. Si en el futuro se necesitan otros formatos, ampliar.
    /// </summary>
    private static readonly Regex BarcodePattern = new(@"^\d{8,14}$", RegexOptions.Compiled);

    private readonly IExternalProductLookupService _lookup;

    public LookupExternalProductoHandler(IExternalProductLookupService lookup)
    {
        _lookup = lookup;
    }

    public async Task<LookupExternalProductoResult> Handle(
        LookupExternalProductoQuery query,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Barcode))
            throw new MissingProductFieldException("codigoBarras");

        var trimmed = query.Barcode.Trim();
        if (!BarcodePattern.IsMatch(trimmed))
            throw new ArgumentException(
                $"Código de barras inválido: '{trimmed}'. Debe contener entre 8 y 14 dígitos.",
                nameof(query));

        return await _lookup.LookupAsync(trimmed, ct);
    }
}
