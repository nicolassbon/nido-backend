namespace Nido.Application.Finanzas;

public sealed class GetInsightsHandler
{
    private static readonly Dictionary<string, string> CatEmoji = new()
    {
        ["Comida"]     = "🍽️",
        ["Transporte"] = "🚗",
        ["Servicios"]  = "💡",
        ["Otros"]      = "📦",
    };

    private static readonly Dictionary<string, string> CatAccion = new()
    {
        ["Comida"]     = "Planificar el menú semanal puede ayudarte a reducir este gasto.",
        ["Transporte"] = "¿Podés combinar viajes o usar transporte público algunos días?",
        ["Servicios"]  = "Revisá si hay suscripciones que ya no estás usando.",
        ["Otros"]      = "Revisá los gastos sin categoría para encontrar oportunidades.",
    };

    private readonly IFinanzasRepository _repo;

    public GetInsightsHandler(IFinanzasRepository repo)
    {
        _repo = repo;
    }

    public async Task<InsightsListResult> Handle(Guid hogarId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var primerDiaMesActual = new DateOnly(now.Year, now.Month, 1);

        // Load current month and 3 previous months
        var gastosMesActual = await _repo.GetGastosAsync(
            new GetGastosQuery(hogarId, primerDiaMesActual.ToString("yyyy-MM-dd"), today.ToString("yyyy-MM-dd"), null), ct);

        var mesesAnteriores = new List<Dictionary<string, decimal>>();
        for (int i = 1; i <= 3; i++)
        {
            var primerDia = primerDiaMesActual.AddMonths(-i);
            var ultimoDia = primerDiaMesActual.AddMonths(-i + 1).AddDays(-1);
            var gastos = await _repo.GetGastosAsync(
                new GetGastosQuery(hogarId, primerDia.ToString("yyyy-MM-dd"), ultimoDia.ToString("yyyy-MM-dd"), null), ct);

            mesesAnteriores.Add(gastos.Gastos
                .GroupBy(g => g.Categoria ?? "Otros")
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Monto)));
        }

        var porCatActual = gastosMesActual.Gastos
            .GroupBy(g => g.Categoria ?? "Otros")
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Monto));

        var mesAnterior = mesesAnteriores[0]; // mes -1

        var alertas    = new List<InsightResult>();
        var tendencias = new List<InsightResult>();
        var positivos  = new List<InsightResult>();

        foreach (var (cat, montoActual) in porCatActual)
        {
            if (!mesAnterior.TryGetValue(cat, out var montoAnterior) || montoAnterior == 0) continue;

            var pct = (int)Math.Round((montoActual - montoAnterior) / montoAnterior * 100m);
            var emoji  = CatEmoji.GetValueOrDefault(cat, "📋");
            CatAccion.TryGetValue(cat, out var accion);

            // Check growth trend across 3 months
            var tendenciaMeses = 0;
            decimal prev = montoAnterior;
            foreach (var mes in mesesAnteriores.Skip(1))
            {
                if (mes.TryGetValue(cat, out var montoPrev) && prev > montoPrev)
                    tendenciaMeses++;
                else
                    break;
                prev = montoPrev;
            }
            if (montoActual > montoAnterior) tendenciaMeses++;

            if (pct >= 20)
            {
                alertas.Add(new InsightResult(
                    Tipo: "alerta",
                    Categoria: cat,
                    Titulo: $"Tu gasto en {cat} subió un {pct}% este mes.",
                    Detalle: $"Pasaste de {FormatARS(montoAnterior)} el mes pasado a {FormatARS(montoActual)} este mes.",
                    Accion: accion,
                    Emoji: emoji,
                    VariacionPct: pct,
                    TendenciaMeses: tendenciaMeses));
            }
            else if (tendenciaMeses >= 2)
            {
                tendencias.Add(new InsightResult(
                    Tipo: "tendencia",
                    Categoria: cat,
                    Titulo: $"{cat} viene creciendo {tendenciaMeses} meses seguidos.",
                    Detalle: $"El mes pasado gastaste {FormatARS(montoAnterior)}, hoy vas en {FormatARS(montoActual)}.",
                    Accion: accion,
                    Emoji: "📈",
                    VariacionPct: pct,
                    TendenciaMeses: tendenciaMeses));
            }
            else if (pct <= -20)
            {
                positivos.Add(new InsightResult(
                    Tipo: "positivo",
                    Categoria: cat,
                    Titulo: $"¡Bajaron un {Math.Abs(pct)}% en {cat}!",
                    Detalle: $"Pasaste de {FormatARS(montoAnterior)} el mes pasado a {FormatARS(montoActual)} este mes. ¡Muy bien!",
                    Accion: null,
                    Emoji: "🎉",
                    VariacionPct: pct,
                    TendenciaMeses: 0));
            }
            else if (Math.Abs(pct) < 10)
            {
                positivos.Add(new InsightResult(
                    Tipo: "positivo",
                    Categoria: cat,
                    Titulo: $"{cat} estable este mes.",
                    Detalle: "Sin variación significativa respecto al mes anterior.",
                    Accion: null,
                    Emoji: "✅",
                    VariacionPct: pct,
                    TendenciaMeses: 0));
            }
        }

        // Prioritize: alerta > tendencia > positivo, max 3 total
        var insights = alertas
            .OrderByDescending(i => i.VariacionPct)
            .Concat(tendencias.OrderByDescending(i => i.TendenciaMeses))
            .Concat(positivos)
            .Take(3)
            .ToList();

        return new InsightsListResult(insights);
    }

    private static string FormatARS(decimal monto) =>
        monto.ToString("C0", new System.Globalization.CultureInfo("es-AR"));
}
