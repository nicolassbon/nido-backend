using Nido.Application.Recetas;

namespace Nido.Application.Finanzas;

public sealed class GetRecomendacionesHandler
{
    private static readonly IReadOnlyList<string> Tips =
    [
        "Planificá las comidas de la semana antes de ir al super para evitar compras impulsivas.",
        "Comprá frutas y verduras de estación: son más baratas y más frescas.",
        "Revisá la alacena antes de salir a comprar para no duplicar lo que ya tenés.",
        "Comparar precios entre marcas puede ahorrarte hasta un 30% en productos de limpieza.",
        "Cocinar en grandes cantidades y freezar porciones reduce el gasto semanal.",
        "Las legumbres (lentejas, garbanzos, porotos) son una proteína económica y nutritiva.",
        "Evitá comprar productos de marca en alimentos básicos como aceite, harina o arroz.",
    ];

    private readonly IRecetaRepository _recetaRepository;

    public GetRecomendacionesHandler(IRecetaRepository recetaRepository)
    {
        _recetaRepository = recetaRepository;
    }

    public async Task<RecomendacionResult> Handle(Guid hogarId, CancellationToken ct)
    {
        var todasLasRecetas = await _recetaRepository.GetAllAsync(hogarId, ct);

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

        return new RecomendacionResult(recetasRecomendadas, Tips);
    }
}
