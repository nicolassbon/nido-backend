namespace Nido.Application.Finanzas;

public sealed record FacturaFileUpload(string FileName, string ContentType, byte[] Content);

public sealed record CreateFacturaCommand(
    Guid HogarId,
    Guid CreadoPor,
    string Nombre,
    string Tipo,
    decimal? Monto,
    string? FechaVencimiento,
    FacturaFileUpload? Archivo
);
