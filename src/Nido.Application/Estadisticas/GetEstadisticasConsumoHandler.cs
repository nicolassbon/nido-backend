namespace Nido.Application.Estadisticas;

public sealed record GetEstadisticasConsumoQuery(Guid HogarId);

public sealed class GetEstadisticasConsumoHandler
{
    private const int DiasUrgenteAgotamiento = 3;
    private const int DiasProntoAgotamiento  = 7;
    private const int DiasPorVencer          = 7;

    private readonly IEstadisticasRepository _repository;

    public GetEstadisticasConsumoHandler(IEstadisticasRepository repository)
    {
        _repository = repository;
    }

    public async Task<EstadisticasConsumoResult> Handle(GetEstadisticasConsumoQuery query, CancellationToken ct)
    {
        var snapshot = await _repository.GetStockSnapshotAsync(query.HogarId, ct);
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var hoyDateTime = DateTime.UtcNow;

        var items = snapshot.Select(s => BuildItem(s, hoy, hoyDateTime)).ToList();

        return new EstadisticasConsumoResult(
            Total:            items.Count,
            TotalAbiertos:    items.Count(i => i.EstaAbierto),
            TotalPorVencer:   items.Count(i => i.DiasHastaVencimiento is >= 0 and <= DiasPorVencer),
            TotalParaReponer: items.Count(i => i.PrioridadReposicion is "urgente" or "pronto"),
            Items:            items);
    }

    private static ProductoConsumoItem BuildItem(StockSnapshot s, DateOnly hoy, DateTime hoyDateTime)
    {
        int? diasHastaVencimiento = s.FechaVencimiento.HasValue
            ? s.FechaVencimiento.Value.DayNumber - hoy.DayNumber
            : null;

        // Tasa de consumo estimada: cuanto bajamos por dia desde que se cargo el producto.
        // Usamos porcentaje_consumido + cantidad para inferir la cantidad original del envase.
        var diasEnStock = Math.Max(1.0, (hoyDateTime - s.CreatedAt).TotalDays);

        decimal? tasaConsumoDiaria = null;
        if (s.PorcentajeConsumido > 0m && s.CantidadActual > 0m)
        {
            var cantidadOriginal = s.PorcentajeConsumido < 100m
                ? s.CantidadActual / ((100m - s.PorcentajeConsumido) / 100m)
                : s.CantidadActual;
            var consumido = cantidadOriginal - s.CantidadActual;
            tasaConsumoDiaria = consumido / (decimal)diasEnStock;
        }

        int? diasHastaAgotamiento = (tasaConsumoDiaria.HasValue && tasaConsumoDiaria.Value > 0m && s.CantidadActual > 0m)
            ? (int?)Math.Ceiling((double)(s.CantidadActual / tasaConsumoDiaria.Value))
            : null;

        // Prioridad de reposicion
        string prioridad = "ninguna";
        if (diasHastaVencimiento is >= 0 and <= DiasUrgenteAgotamiento) prioridad = "urgente";
        else if (diasHastaAgotamiento is >= 0 and <= DiasUrgenteAgotamiento) prioridad = "urgente";
        else if (diasHastaVencimiento is >= 0 and <= DiasProntoAgotamiento) prioridad = "pronto";
        else if (diasHastaAgotamiento is >= 0 and <= DiasProntoAgotamiento) prioridad = "pronto";
        else if (s.EstaAbierto && s.PorcentajeConsumido >= 75m) prioridad = "pronto";
        else if (s.EstaAbierto && s.PorcentajeConsumido >= 50m) prioridad = "normal";

        return new ProductoConsumoItem(
            StockId:                       s.StockId,
            ProductoId:                    s.ProductoId,
            Nombre:                        s.Nombre,
            CategoriaNombre:               s.CategoriaNombre,
            Ubicacion:                     s.Ubicacion,
            CantidadActual:                s.CantidadActual,
            UnidadMedida:                  s.UnidadMedida,
            EstaAbierto:                   s.EstaAbierto,
            PorcentajeConsumido:           s.PorcentajeConsumido,
            FechaVencimiento:              s.FechaVencimiento?.ToString("yyyy-MM-dd"),
            DiasHastaVencimiento:          diasHastaVencimiento,
            TasaConsumoDiariaEstimada:     tasaConsumoDiaria.HasValue ? Math.Round(tasaConsumoDiaria.Value, 3) : null,
            DiasHastaAgotamientoEstimado:  diasHastaAgotamiento,
            PrioridadReposicion:           prioridad);
    }
}
