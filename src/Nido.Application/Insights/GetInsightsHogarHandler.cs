using System.Globalization;
using Nido.Application.Alacena;

namespace Nido.Application.Insights;

/// <summary>
/// Motor de recomendaciones realista basado en historial del hogar.
///
/// Predicción se basa en MEDIANA de días entre compras del mismo producto
/// (no en tasa diaria que ignora cómo compran las familias).
///
/// Productos se clasifican en 4 categorías:
///  - Esencial : ≥3 compras, casi nunca se vencen → alerta fuerte si stock=0
///  - Recurrente: 2 compras
///  - Ocasional: 1 compra
///  - Caprichoso: tasa desperdicio ≥30% → sugerir comprar menos
/// </summary>
public sealed class GetInsightsHogarHandler
{
    private const int VentanaConsumoDias = 30;
    private const int VentanaHistoricaDias = 90;
    private const int DiasUmbralVencimiento = 7;
    private const int DiasUmbralComprarPronto = 5;
    private const int VencidosUmbralDesperdicio = 2;
    private const int DiasMinEnvaseZombie = 14;
    private const int IngredientesTopN = 5;
    private const int RecetasTopN = 3;

    private static readonly string[] DiasSemanaEs =
    {
        "domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado"
    };

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
        var consumos = await _consumos.GetConsumosPorProductoAsync(query.HogarId, VentanaHistoricaDias, ct);
        var compras = await _consumos.GetComprasPorProductoAsync(query.HogarId, VentanaHistoricaDias, ct);
        var envasesZombiesRaw = await _consumos.GetEnvasesAbiertosLargoAsync(query.HogarId, DiasMinEnvaseZombie, ct);
        var cocinadasPorDia = await _consumos.GetCocinadasPorDiaSemanaAsync(query.HogarId, VentanaHistoricaDias, ct);
        var recetasTop = await _consumos.GetRecetasTopAsync(query.HogarId, VentanaHistoricaDias, RecetasTopN, ct);
        var fechasComprasHogar = await _consumos.GetFechasComprasHogarAsync(query.HogarId, VentanaHistoricaDias, ct);

        var consumos30 = await _consumos.GetConsumosPorProductoAsync(query.HogarId, VentanaConsumoDias, ct);
        var consumosMesAnterior = await GetConsumosVentanaPasadaAsync(query.HogarId, ct);

        var consumoIdx = consumos
            .GroupBy(c => Normalizar(c.ProductoNombre))
            .ToDictionary(g => g.Key, g => g.First());

        var compraIdx = compras
            .GroupBy(c => Normalizar(c.ProductoNombre))
            .ToDictionary(g => g.Key, g => g.First());

        var clasificacion = ClasificarProductos(compras, consumos);
        var clasifPorNombre = ConstruirIndiceClasificacion(clasificacion);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var porVencer = stock
            .Select(s => new
            {
                Item = s,
                Fecha = ParseFecha(s.FechaVencimiento)
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

        var comprarPronto = ComputeComprarPronto(stock, compraIdx, consumoIdx, clasifPorNombre);

        var desperdicios = consumos
            .Where(c => c.VecesVencido >= VencidosUmbralDesperdicio)
            .OrderByDescending(c => c.VecesVencido)
            .Take(5)
            .Select(c =>
            {
                var totalEventos = c.Eventos == 0 ? 1 : c.Eventos;
                var tasa = Math.Round((decimal)c.VecesVencido / totalEventos * 100m, 1);
                return new DesperdicioItem(
                    c.ProductoNombre, c.VecesVencido, c.VecesCocinado, tasa,
                    ConstruirSugerencia(c, tasa));
            })
            .ToList();

        var envasesZombies = envasesZombiesRaw
            .Select(z => new EnvaseZombieItem(
                z.StockHogarId, z.ProductoNombre,
                (int)Math.Round((DateTime.UtcNow - z.UltimaActualizacion).TotalDays),
                "Envase abierto hace mucho. Revisá si todavía está en buen estado o terminalo pronto."))
            .OrderByDescending(z => z.DiasAbierto)
            .Take(5)
            .ToList();

        var ingredientesTop = consumos
            .Where(c => c.VecesCocinado > 0)
            .OrderByDescending(c => c.VecesCocinado)
            .Take(IngredientesTopN)
            .Select(c => c.ProductoNombre)
            .ToList();

        var (diaTop, cocinadasDiaTop) = CalcularDiaTop(cocinadasPorDia);
        var patrones = new PatronesCocina(diaTop, cocinadasDiaTop, ingredientesTop, recetasTop);

        var totalEventos30 = consumos30.Sum(c => c.Eventos);
        var totalVencidos30 = consumos30.Sum(c => c.VecesVencido);
        var tasaDesperdicio = totalEventos30 > 0
            ? Math.Round((decimal)totalVencidos30 / totalEventos30 * 100m, 1)
            : 0m;

        var tasaDesperdicioPrev = consumosMesAnterior.Eventos > 0
            ? Math.Round((decimal)consumosMesAnterior.Vencidos / consumosMesAnterior.Eventos * 100m, 1)
            : 0m;

        var frecuenciaHogar = FrecuenciaMedianaDias(fechasComprasHogar.OrderBy(f => f).ToList());

        var resumen = new ResumenInsights(
            stock.Count,
            porVencer.Count,
            totalEventos30,
            tasaDesperdicio,
            tasaDesperdicioPrev,
            frecuenciaHogar,
            clasificacion.Esenciales.Count);

        return new GetInsightsHogarResult(
            comprarPronto, porVencer, desperdicios, envasesZombies, clasificacion, patrones, resumen);
    }

    /// <summary>
    /// Predicción de agotamiento por producto, para consumo fuera de la pantalla de Insights
    /// (ej. alertas proactivas). Reusa la misma lógica de mediana de días entre compras.
    /// </summary>
    public async Task<IReadOnlyList<ComprarProntoItem>> GetComprarProntoAsync(Guid hogarId, CancellationToken ct)
    {
        var stock = await _alacena.GetByHogarAsync(hogarId, ct);
        var consumos = await _consumos.GetConsumosPorProductoAsync(hogarId, VentanaHistoricaDias, ct);
        var compras = await _consumos.GetComprasPorProductoAsync(hogarId, VentanaHistoricaDias, ct);

        var consumoIdx = consumos
            .GroupBy(c => Normalizar(c.ProductoNombre))
            .ToDictionary(g => g.Key, g => g.First());

        var compraIdx = compras
            .GroupBy(c => Normalizar(c.ProductoNombre))
            .ToDictionary(g => g.Key, g => g.First());

        var clasificacion = ClasificarProductos(compras, consumos);
        var clasifPorNombre = ConstruirIndiceClasificacion(clasificacion);

        return ComputeComprarPronto(stock, compraIdx, consumoIdx, clasifPorNombre);
    }

    private static IReadOnlyList<ComprarProntoItem> ComputeComprarPronto(
        IReadOnlyList<StockItemResult> stock,
        Dictionary<string, ComprasPorProducto> compraIdx,
        Dictionary<string, ConsumoPorProducto> consumoIdx,
        Dictionary<string, string> clasifPorNombre)
    {
        return stock
            .Where(s => s.Cantidad > 0)
            .Select(s =>
            {
                var clave = Normalizar(s.Nombre);
                if (!compraIdx.TryGetValue(clave, out var compraInfo)) return null;
                if (!consumoIdx.TryGetValue(clave, out var consumoInfo) || consumoInfo.VecesCocinado == 0) return null;

                var frecuenciaDias = FrecuenciaMedianaDias(compraInfo.FechasCompra);
                if (frecuenciaDias <= 0) return null;

                var diasDesdeUltimaCompra = (int)Math.Round((DateTime.UtcNow - compraInfo.FechasCompra[^1]).TotalDays);
                var diasParaAgotar = Math.Max(0, frecuenciaDias - diasDesdeUltimaCompra);

                if (diasParaAgotar > DiasUmbralComprarPronto) return null;

                var categoria = clasifPorNombre.GetValueOrDefault(clave) ?? "Recurrente";

                return new ComprarProntoItem(
                    s.Id,
                    s.Nombre,
                    s.Cantidad,
                    s.UnidadMedida,
                    diasParaAgotar,
                    frecuenciaDias,
                    categoria);
            })
            .Where(x => x is not null)
            .Cast<ComprarProntoItem>()
            .OrderBy(x => x.DiasParaAgotar)
            .Take(10)
            .ToList();
    }

    private async Task<(int Eventos, int Vencidos)> GetConsumosVentanaPasadaAsync(Guid hogarId, CancellationToken ct)
    {
        // Ventana 30-60 días atrás. Reusamos el método existente trayendo 60 días
        // y restando el último mes.
        var ult60 = await _consumos.GetConsumosPorProductoAsync(hogarId, 60, ct);
        var ult30 = await _consumos.GetConsumosPorProductoAsync(hogarId, 30, ct);

        var ev60 = ult60.Sum(c => c.Eventos);
        var ven60 = ult60.Sum(c => c.VecesVencido);
        var ev30 = ult30.Sum(c => c.Eventos);
        var ven30 = ult30.Sum(c => c.VecesVencido);

        return (Math.Max(0, ev60 - ev30), Math.Max(0, ven60 - ven30));
    }

    private static ClasificacionProductos ClasificarProductos(
        IReadOnlyList<ComprasPorProducto> compras,
        IReadOnlyList<ConsumoPorProducto> consumos)
    {
        var consumoIdx = consumos
            .GroupBy(c => Normalizar(c.ProductoNombre))
            .ToDictionary(g => g.Key, g => g.First());

        var esenciales = new List<ProductoClasificado>();
        var recurrentes = new List<ProductoClasificado>();
        var ocasionales = new List<ProductoClasificado>();
        var caprichosos = new List<ProductoClasificado>();

        foreach (var c in compras)
        {
            var clave = Normalizar(c.ProductoNombre);
            var vecesComprado = c.FechasCompra.Count;
            consumoIdx.TryGetValue(clave, out var consumoInfo);

            var vecesVencido = consumoInfo?.VecesVencido ?? 0;
            var totalEventos = consumoInfo?.Eventos ?? 0;
            var tasaDesp = totalEventos > 0 ? (decimal)vecesVencido / totalEventos : 0m;
            var frecuencia = FrecuenciaMedianaDias(c.FechasCompra);

            var item = new ProductoClasificado(c.ProductoNombre, vecesComprado, vecesVencido, frecuencia);

            if (tasaDesp >= 0.30m && vecesVencido >= 1)
                caprichosos.Add(item);
            else if (vecesComprado >= 3 && vecesVencido == 0)
                esenciales.Add(item);
            else if (vecesComprado == 2)
                recurrentes.Add(item);
            else
                ocasionales.Add(item);
        }

        return new ClasificacionProductos(
            esenciales.OrderBy(e => e.FrecuenciaCompraDias).ToList(),
            recurrentes.OrderBy(e => e.ProductoNombre).ToList(),
            ocasionales.OrderBy(e => e.ProductoNombre).ToList(),
            caprichosos.OrderByDescending(e => e.VecesVencido).ToList());
    }

    private static Dictionary<string, string> ConstruirIndiceClasificacion(ClasificacionProductos c)
    {
        var dict = new Dictionary<string, string>();
        foreach (var p in c.Esenciales)   dict[Normalizar(p.ProductoNombre)] = "Esencial";
        foreach (var p in c.Recurrentes)  dict[Normalizar(p.ProductoNombre)] = "Recurrente";
        foreach (var p in c.Ocasionales)  dict[Normalizar(p.ProductoNombre)] = "Ocasional";
        foreach (var p in c.Caprichosos)  dict[Normalizar(p.ProductoNombre)] = "Caprichoso";
        return dict;
    }

    private static int FrecuenciaMedianaDias(IReadOnlyList<DateTime> fechas)
    {
        if (fechas.Count < 2) return 0;
        var ordenadas = fechas.OrderBy(f => f).ToList();
        var diffs = new List<double>();
        for (int i = 1; i < ordenadas.Count; i++)
        {
            diffs.Add((ordenadas[i] - ordenadas[i - 1]).TotalDays);
        }
        diffs.Sort();
        var mid = diffs.Count / 2;
        var mediana = diffs.Count % 2 == 0
            ? (diffs[mid - 1] + diffs[mid]) / 2.0
            : diffs[mid];
        return (int)Math.Round(mediana);
    }

    private static (string?, int) CalcularDiaTop(IReadOnlyDictionary<DayOfWeek, int> dist)
    {
        if (dist.Count == 0) return (null, 0);
        var top = dist.OrderByDescending(kv => kv.Value).First();
        return (DiasSemanaEs[(int)top.Key], top.Value);
    }

    private static DateOnly? ParseFecha(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
    }

    private static string Normalizar(string s) => s.Trim().ToLowerInvariant();

    private static string ConstruirSugerencia(ConsumoPorProducto c, decimal tasaDesperdicioPorc)
    {
        if (c.VecesCocinado == 0)
            return $"Lo compraste {c.VecesVencido} {(c.VecesVencido == 1 ? "vez" : "veces")} y se venció sin usarse. Considerá no comprarlo más.";

        if (tasaDesperdicioPorc >= 50m)
            return $"{tasaDesperdicioPorc}% de las veces se vence. Probá comprar la mitad de cantidad.";

        return $"Se venció {c.VecesVencido} {(c.VecesVencido == 1 ? "vez" : "veces")}. Usalo en recetas antes de la próxima compra.";
    }
}
