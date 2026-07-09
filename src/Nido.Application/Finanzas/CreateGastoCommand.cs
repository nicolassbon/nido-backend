namespace Nido.Application.Finanzas;

public sealed record CreateGastoCommand(
    Guid HogarId,
    Guid PagadoPorId,
    decimal Monto,
    string? Descripcion,
    string? Categoria,
    string Fecha,
    bool EsCompartido = true,
    IReadOnlyList<Guid>? ParticipantesIds = null,
    Guid? FacturaId = null
);
