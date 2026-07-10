namespace Nido.Api.Contracts.Finanzas;

public sealed record UpdateGastoRequest(
    decimal Monto,
    string? Descripcion,
    string? Categoria,
    string Fecha,
    Guid? PagadoPorId,
    bool EsCompartido = true,
    List<Guid>? ParticipantesIds = null
);

public sealed record DeleteGastoResponse(bool FacturaRevertida, Guid? FacturaId);
