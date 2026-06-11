using Nido.Application.Common.ProfileImages;
using Nido.Application.Finanzas.Exceptions;

namespace Nido.Application.Finanzas;

public sealed class CreateFacturaHandler
{
    private static readonly HashSet<string> TiposValidos = ["servicio", "suscripcion"];
    private static readonly HashSet<string> ContentTypesValidos =
        ["image/jpeg", "image/png", "image/webp", "application/pdf"];

    private readonly IFinanzasRepository _repository;
    private readonly IProfileImageStorage _fileStorage;
    private readonly IProfileImagePublicUrlResolver _urlResolver;

    public CreateFacturaHandler(
        IFinanzasRepository repository,
        IProfileImageStorage fileStorage,
        IProfileImagePublicUrlResolver urlResolver)
    {
        _repository = repository;
        _fileStorage = fileStorage;
        _urlResolver = urlResolver;
    }

    public async Task<FacturaResult> Handle(CreateFacturaCommand command, CancellationToken ct)
    {
        if (!TiposValidos.Contains(command.Tipo))
            throw new InvalidFacturaTipoException(command.Tipo);

        if (command.Monto.HasValue && command.Monto <= 0)
            throw new InvalidGastoMontoException();

        if (!string.IsNullOrWhiteSpace(command.FechaVencimiento) && !DateOnly.TryParse(command.FechaVencimiento, out _))
            throw new InvalidGastoFechaException();

        if (command.Archivo is not null && !ContentTypesValidos.Contains(command.Archivo.ContentType))
            throw new InvalidFacturaArchivoException();

        var facturaId = Guid.NewGuid();
        string? storageKey = null;

        if (command.Archivo is not null)
        {
            var ext = command.Archivo.ContentType switch
            {
                "application/pdf" => "pdf",
                "image/png" => "png",
                "image/webp" => "webp",
                _ => "jpg"
            };
            storageKey = $"facturas/{command.HogarId}/{facturaId}.{ext}";
            await _fileStorage.UploadAsync(storageKey, command.Archivo.Content, command.Archivo.ContentType, ct);
        }

        return await _repository.CreateFacturaAsync(command, facturaId, storageKey, ct);
    }
}
