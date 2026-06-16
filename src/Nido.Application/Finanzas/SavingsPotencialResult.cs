namespace Nido.Application.Finanzas;

public sealed record SavingsPotencialResult(
    decimal TotalPotencial,
    int PeriodoBaseMeses,
    IReadOnlyList<SavingsCategoriaResult> Categorias
);

public sealed record SavingsCategoriaResult(
    string Nombre,
    decimal GastoActual,
    decimal MediaHistorica,
    decimal PotencialAhorro,
    int VariacionPct,
    string Color
);
