namespace Nido.Application.Finanzas;

public sealed record FacturaResult(
    Guid Id,
    string Nombre,
    string Tipo,
    decimal? Monto,
    string? FechaVencimiento,
    string? ArchivoUrl,
    bool Pagada,
    int? DiasParaVencer,
    Guid CreadoPor,
    string CreadoPorNombre,
    DateTime CreatedAt
);
