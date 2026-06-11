using Nido.Application.Common.ProfileImages;
using Nido.Application.Finanzas.Exceptions;

namespace Nido.Application.Finanzas;

public sealed class DeleteFacturaHandler
{
    private readonly IFinanzasRepository _repository;
    private readonly IProfileImageStorage _fileStorage;

    public DeleteFacturaHandler(IFinanzasRepository repository, IProfileImageStorage fileStorage)
    {
        _repository = repository;
        _fileStorage = fileStorage;
    }

    public async Task Handle(Guid facturaId, Guid hogarId, CancellationToken ct)
    {
        var storageKey = await _repository.DeleteFacturaAsync(facturaId, hogarId, ct);
        if (storageKey is null) throw new FacturaNotFoundException();

        if (!string.IsNullOrWhiteSpace(storageKey))
        {
            try { await _fileStorage.DeleteAsync(storageKey, ct); }
            catch { /* ignorar si el archivo ya no existe */ }
        }
    }
}
