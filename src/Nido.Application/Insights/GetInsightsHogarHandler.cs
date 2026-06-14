using System.Globalization;
using Nido.Application.Alacena;

namespace Nido.Application.Insights;

public sealed class GetInsightsHogarHandler
{
    private const int DiasVentanaConsumo = 30;
    private const int DiasUmbralVencimiento = 7;
    private const int DiasUmbralComprarPronto = 5;
    private const int VencidosUmbralDesperdicio = 2;

    private readonly IAlacenaRepository _alacena;
    private readonly IConsumoProductoRepository _consumos;

    public GetInsightsHogarHandler(
        IAlacenaRepository alacena,
        IConsumoProductoRepository consumos)
    {
        _alacena = alacena;
        _consumos = consumos;
    }

    public async Task<GetInsightsHogarResult> Handle(GetInsightsHogarQuery query, CancellationToken ct)
    {
        var stock = await _alacena.GetByHogarAsync(query.HogarId, ct);
        var consumos = await _consumos.GetConsumosPorProductoAsync(
            query.HogarId, DiasVentanaConsumo, ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var porVencer = stock
            .Where(s => !string.IsNullOrWhiteSpace(s.FechaVencimiento))
            .Select(s => new
            {
                Item = s,
                Fecha = ParseFecha(s.FechaVencimiento!)
            })
            .Where(x => x.Fecha.HasValue)
            .Select(x => new
            {
                x.Item,
                Dias = x.Fecha!.Value.DayNumber - today.DayNumber,
                FechaStr = x.Fecha.Value.ToString("yyyy-MM-dd")
            })
            .Where(x => x.Dias >= 0 && x.Dias <= DiasUmbralVencimiento)
            .OrderBy(x => x.Dias)
            .Take(10)
            .Select(x => new PorVencerItem(
                x.Item.Id,
                x.Item.Nombre,
                x.Item.Imagen,
                x.Item.Cantidad,
                x.Item.UnidadMedida,
                x.FechaStr,
                x.Dias))
            .ToList();

        var consumosPorNombre = consumos
            .GroupBy(c => Normalizar(c.ProductoNombre))
            .ToDictionary(g => g.Key, g => g.First());

        var comprarPronto = stock
            .Where(s => s.Cantidad > 0)
            .Select(s =>
            {
                var clave = Normalizar(s.Nombre);
                if (!consumosPorNombre.TryGetValue(clave, out var c) || c.VecesCocinado == 0)
                    return null;

                var tasaDiaria = c.CantidadTotal / DiasVentanaConsumo;
                if (tasaDiaria <= 0) return null;

                var diasParaAgotar = (double)(s.Cantidad / tasaDiaria);
                if (diasParaAgotar > DiasUmbralComprarPronto) return null;

                return new ComprarProntoItem(
                    s.Nombre,
                    s.Cantidad,
                    s.UnidadMedida,
                    Math.Round(diasParaAgotar, 1),
                    Math.Round(tasaDiaria, 2));
            })
            .Where(x => x is not null)
            .Cast<ComprarProntoItem>()
            .OrderBy(x => x.DiasParaAgotar)
            .Take(10)
            .ToList();

        var desperdicios = consumos
            .Where(c => c.VecesVencido >= VencidosUmbralDesperdicio)
            .OrderByDescending(c => c.VecesVencido)
            .Take(5)
            .Select(c => new DesperdicioItem(
                c.ProductoNombre,
                c.VecesVencido,
                c.VecesCocinado,
                ConstruirSugerencia(c)))
            .ToList();

        var totalEventos = consumos.Sum(c => c.Eventos);
        var totalVencidos = consumos.Sum(c => c.VecesVencido);
        var tasaDesperdicio = totalEventos > 0
            ? Math.Round((decimal)totalVencidos / totalEventos * 100m, 1)
            : 0m;

        var resumen = new ResumenInsights(
            stock.Count,
            porVencer.Count,
            totalEventos,
            tasaDesperdicio);

        return new GetInsightsHogarResult(comprarPronto, porVencer, desperdicios, resumen);
    }

    private static DateOnly? ParseFecha(string raw)
    {
        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        return null;
    }

    private static string Normalizar(string s) => s.Trim().ToLowerInvariant();

    private static string ConstruirSugerencia(ConsumoPorProducto c)
    {
        if (c.VecesCocinado == 0)
            return $"Compraste {c.VecesVencido} veces y se venció sin usarse. Considerá no comprarlo o comprar menos cantidad.";

        return $"Se venció {c.VecesVencido} veces este mes. Probá comprar la mitad o usalo en recetas antes.";
    }
}
