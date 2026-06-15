namespace Nido.Api.Contracts.Finanzas;

public sealed record SavingsCategoriaResponse(
    string Nombre,
    decimal GastoActual,
    decimal MediaHistorica,
    decimal PotencialAhorro,
    int VariacionPct,
    string Color
);

public sealed record SavingsPotencialResponse(
    decimal TotalPotencial,
    int PeriodoBaseMeses,
    IReadOnlyList<SavingsCategoriaResponse> Categorias
);
