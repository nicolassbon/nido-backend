namespace Nido.Api.Contracts.Finanzas;

public sealed record CreateGastoRequest(
    decimal Monto,
    string? Descripcion,
    string? Categoria,
    string Fecha,
    Guid? PagadoPorId,
    bool EsCompartido = true,
    List<Guid>? ParticipantesIds = null
);
