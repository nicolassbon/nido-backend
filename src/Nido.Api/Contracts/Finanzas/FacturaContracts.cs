using Microsoft.AspNetCore.Http;

namespace Nido.Api.Contracts.Finanzas;

public sealed record CreateFacturaRequest(
    string Nombre,
    string Tipo,
    decimal? Monto,
    string? FechaVencimiento,
    IFormFile? Archivo
);

public sealed record FacturaResponse(
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

public sealed record PagarFacturaResponse(FacturaResponse Factura, GastoResponse? GastoCreado);
