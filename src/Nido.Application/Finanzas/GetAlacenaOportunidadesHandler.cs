using Nido.Application.Alacena;
using Nido.Application.Recetas;

namespace Nido.Application.Finanzas;

public sealed class GetAlacenaOportunidadesHandler
{
    private readonly IAlacenaRepository _alacenaRepository;
    private readonly IRecetaRepository  _recetaRepository;

    public GetAlacenaOportunidadesHandler(
        IAlacenaRepository alacenaRepository,
        IRecetaRepository recetaRepository)
    {
        _alacenaRepository = alacenaRepository;
        _recetaRepository  = recetaRepository;
    }

    public async Task<AlacenaOportunidadesResult> Handle(Guid hogarId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var items = await _alacenaRepository.GetByHogarAsync(hogarId, ct);

        var disponibles = items
            .Where(i => i.PorcentajeConsumido < 90m)
            .Where(i =>
            {
                if (i.FechaVencimiento is null) return true;
                return DateOnly.TryParse(i.FechaVencimiento, out var fecha) && fecha >= today;
            })
            .ToList();

        var destacados = disponibles
            .OrderBy(i =>
            {
                if (i.FechaVencimiento is null || !DateOnly.TryParse(i.FechaVencimiento, out var fecha))
                    return int.MaxValue;
                return (fecha.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days;
            })
            .Take(5)
            .Select(i =>
            {
                int? diasParaVencer = null;
                if (i.FechaVencimiento is not null && DateOnly.TryParse(i.FechaVencimiento, out var fecha))
                    diasParaVencer = (fecha.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days;

                return new ProductoDestacadoResult(
                    i.Id.ToString(),
                    i.Nombre,
                    i.Ubicacion,
                    diasParaVencer,
                    i.Imagen,
                    (int)i.PorcentajeConsumido);
            })
            .ToList();

        var todasLasRecetas = await _recetaRepository.GetAllAsync(hogarId, ct);
        var recetas = todasLasRecetas
            .Where(r => r.Ingredientes.Count > 0)
            .Select(r => new
            {
                Receta   = r,
                EnStock  = r.Ingredientes.Count(i => i.EnStock),
                Total    = r.Ingredientes.Count,
            })
            .Where(x => x.EnStock > 0)
            .OrderByDescending(x => (double)x.EnStock / x.Total)
            .Take(3)
            .Select(x => new RecetaRecomendadaResult(
                x.Receta.Id,
                x.Receta.Nombre,
                x.Receta.ImagenUrl,
                x.EnStock,
                x.Total))
            .ToList();

        return new AlacenaOportunidadesResult(destacados, recetas, disponibles.Count);
    }
}
