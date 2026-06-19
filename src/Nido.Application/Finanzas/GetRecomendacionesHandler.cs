using Nido.Application.Recetas;

namespace Nido.Application.Finanzas;

public sealed class GetRecomendacionesHandler
{
    private static readonly IReadOnlyList<string> TipsGenericos =
    [
        "Planificá las comidas de la semana antes de ir al super para evitar compras impulsivas.",
        "Comprá frutas y verduras de estación: son más baratas y más frescas.",
        "Revisá la alacena antes de salir a comprar para no duplicar lo que ya tenés.",
        "Comparar precios entre marcas puede ahorrarte hasta un 30% en productos de limpieza.",
        "Cocinar en grandes cantidades y freezar porciones reduce el gasto semanal.",
        "Las legumbres (lentejas, garbanzos, porotos) son una proteína económica y nutritiva.",
        "Evitá comprar productos de marca en alimentos básicos como aceite, harina o arroz.",
    ];

    private readonly IFinanzasRepository _finanzasRepository;
    private readonly IRecetaRepository _recetaRepository;

    public GetRecomendacionesHandler(IFinanzasRepository finanzasRepository, IRecetaRepository recetaRepository)
    {
        _finanzasRepository = finanzasRepository;
        _recetaRepository = recetaRepository;
    }

    public async Task<RecomendacionResult> Handle(Guid hogarId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var primerDiaMes = new DateOnly(now.Year, now.Month, 1);
        var ultimoDiaMes = primerDiaMes.AddMonths(1).AddDays(-1);
        var primerDiaMesAnterior = primerDiaMes.AddMonths(-1);
        var ultimoDiaMesAnterior = primerDiaMes.AddDays(-1);

        var gastosActual = await _finanzasRepository.GetGastosAsync(
            new GetGastosQuery(hogarId, primerDiaMes.ToString("yyyy-MM-dd"), ultimoDiaMes.ToString("yyyy-MM-dd"), null), ct);
        var gastosAnterior = await _finanzasRepository.GetGastosAsync(
            new GetGastosQuery(hogarId, primerDiaMesAnterior.ToString("yyyy-MM-dd"), ultimoDiaMesAnterior.ToString("yyyy-MM-dd"), null), ct);

        var tipsPersonalizados = GenerarTipsPersonalizados(gastosActual, gastosAnterior);

        var todasLasRecetas = await _recetaRepository.GetAllAsync(hogarId, Guid.Empty, ct);
        var recetasRecomendadas = todasLasRecetas
            .Where(r => r.Ingredientes.Count > 0)
            .Select(r => new
            {
                Receta = r,
                EnStock = r.Ingredientes.Count(i => i.EnStock),
                Total = r.Ingredientes.Count
            })
            .Where(x => x.EnStock > 0)
            .OrderByDescending(x => (double)x.EnStock / x.Total)
            .Take(5)
            .Select(x => new RecetaRecomendadaResult(
                x.Receta.Id,
                x.Receta.Nombre,
                x.Receta.ImagenUrl,
                x.EnStock,
                x.Total))
            .ToList();

        var tips = tipsPersonalizados.Concat(TipsGenericos).ToList();
        return new RecomendacionResult(recetasRecomendadas, tips);
    }

    private static List<string> GenerarTipsPersonalizados(GetGastosResult actual, GetGastosResult anterior)
    {
        var porCatActual = actual.Gastos
            .GroupBy(g => g.Categoria ?? "Otros")
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Monto));

        var porCatAnterior = anterior.Gastos
            .GroupBy(g => g.Categoria ?? "Otros")
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Monto));

        var tips = new List<(decimal Magnitud, string Texto)>();

        foreach (var (cat, montoActual) in porCatActual)
        {
            if (!porCatAnterior.TryGetValue(cat, out var montoAnterior) || montoAnterior == 0) continue;

            var pct = (montoActual - montoAnterior) / montoAnterior * 100m;

            if (pct >= 20m)
                tips.Add((pct, $"Gastaron un {pct:0}% más en {cat} este mes que el anterior. Puede ser una buena oportunidad para reducir en esta categoría."));
            else if (pct <= -20m)
                tips.Add((Math.Abs(pct), $"Redujeron los gastos en {cat} un {Math.Abs(pct):0}% respecto al mes anterior. ¡Sigan así!"));
        }

        return tips
            .OrderByDescending(t => t.Magnitud)
            .Take(3)
            .Select(t => t.Texto)
            .ToList();
    }
}
