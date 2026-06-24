namespace Nido.Application.Finanzas;

public sealed class GetSavingsPotencialHandler
{
    private static readonly Dictionary<string, string> CategoryColors = new()
    {
        ["Comida"]     = "#2B4A47",
        ["Transporte"] = "#4a7c59",
        ["Servicios"]  = "#B48B6A",
        ["Otros"]      = "#d4c5b0",
    };

    private readonly IFinanzasRepository _repo;

    public GetSavingsPotencialHandler(IFinanzasRepository repo)
    {
        _repo = repo;
    }

    public async Task<SavingsPotencialResult> Handle(Guid hogarId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var primerDiaMesActual = new DateOnly(now.Year, now.Month, 1);
        var ultimoDiaMesActual = primerDiaMesActual.AddMonths(1).AddDays(-1);

        var gastosMesActual = await _repo.GetGastosAsync(
            new GetGastosQuery(hogarId, primerDiaMesActual.ToString("yyyy-MM-dd"), ultimoDiaMesActual.ToString("yyyy-MM-dd"), null), ct);

        // Query para los ultimos 3 meses completos y luego agrupar por mes en memoria, para evitar problemas con meses que no tengan datos completos
        var inicioHistorico = primerDiaMesActual.AddMonths(-3);
        var finHistorico    = primerDiaMesActual.AddDays(-1);
        var gastosHistoricos = await _repo.GetGastosAsync(
            new GetGastosQuery(hogarId, inicioHistorico.ToString("yyyy-MM-dd"), finHistorico.ToString("yyyy-MM-dd"), null), ct);

        // Agrupa por mes para luego sacar una media mensual por categoria.
        var porMesPorCat = gastosHistoricos.Gastos
            .GroupBy(g => (g.Fecha[..7])) // "yyyy-MM"
            .Select(mesGroup => mesGroup
                .GroupBy(g => g.Categoria ?? "Otros")
                .ToDictionary(cg => cg.Key, cg => cg.Sum(x => x.Monto)))
            .ToList();

        // Promedio por categoria a lo largo de los meses disponibles. Si hay meses sin datos, se ignoran para el calculo del promedio.
        var mesesConDatos = porMesPorCat.Count;
        var mediaHistoricaPorCat = new Dictionary<string, decimal>();
        foreach (var mesDict in porMesPorCat)
        {
            foreach (var (cat, monto) in mesDict)
            {
                mediaHistoricaPorCat.TryGetValue(cat, out var acc);
                mediaHistoricaPorCat[cat] = acc + monto;
            }
        }
        if (mesesConDatos > 0)
        {
            foreach (var key in mediaHistoricaPorCat.Keys.ToList())
                mediaHistoricaPorCat[key] = Math.Round(mediaHistoricaPorCat[key] / mesesConDatos, 2);
        }

        var porCatActual = gastosMesActual.Gastos
            .GroupBy(g => g.Categoria ?? "Otros")
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Monto));

        var categorias = new List<SavingsCategoriaResult>();

        foreach (var (cat, gastoActual) in porCatActual)
        {
            if (!mediaHistoricaPorCat.TryGetValue(cat, out var media) || media == 0) continue;

            var variacionPct = (int)Math.Round((gastoActual - media) / media * 100m);
            if (variacionPct <= 10) continue;

            var potencial = Math.Max(0m, Math.Round(gastoActual - media, 2));
            var color = CategoryColors.GetValueOrDefault(cat, "#d4c5b0");

            categorias.Add(new SavingsCategoriaResult(cat, gastoActual, media, potencial, variacionPct, color));
        }

        categorias.Sort((a, b) => b.PotencialAhorro.CompareTo(a.PotencialAhorro));

        return new SavingsPotencialResult(
            Math.Round(categorias.Sum(c => c.PotencialAhorro), 2),
            mesesConDatos > 0 ? mesesConDatos : 3,
            categorias);
    }
}
