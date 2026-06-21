namespace Nido.Application.Alacena;

public static class StockLoadOrigins
{
    public const string Manual = "manual";
    public const string CodigoBarras = "codigo_barras";
    public const string TicketCompra = "ticket_compra";

    public static string Normalize(string? value, string? codigoBarras)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.IsNullOrWhiteSpace(codigoBarras) ? Manual : CodigoBarras;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized is Manual or CodigoBarras or TicketCompra
            ? normalized
            : throw new ArgumentException("El origen de carga debe ser 'manual', 'codigo_barras' o 'ticket_compra'.");
    }
}
