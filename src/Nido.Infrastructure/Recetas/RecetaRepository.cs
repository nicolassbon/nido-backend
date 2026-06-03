using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Nido.Application.Recetas;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Recetas;

public sealed class RecetaRepository : IRecetaRepository
{
    private static readonly IReadOnlyDictionary<string, string[]> AllergenAliases = new Dictionary<string, string[]>
    {
        ["Maní"] = ["mani", "cacahuate", "peanut", "peanuts"],
        ["Gluten"] = ["gluten", "harina", "trigo", "fideos", "pasta", "pan", "masa"],
        ["Lactosa"] = ["lactosa", "leche", "queso", "crema", "manteca", "yogur", "yogurt", "milk", "cheese"],
        ["Mariscos"] = ["mariscos", "camaron", "camarones", "langostino", "langostinos", "mejillon", "mejillones", "calamar", "calamares"],
        ["Soja"] = ["soja", "soya", "tofu", "salsa de soja"],
        ["Huevo"] = ["huevo", "huevos", "clara", "yema", "egg"],
        ["Frutos secos"] = ["frutos secos", "almendra", "almendras", "nuez", "nueces", "avellana", "avellanas", "castana", "castanas", "pistacho", "pistachos"],
        ["Pescado"] = ["pescado", "atun", "salmon", "merluza", "sardina", "sardinas"],
        ["Sésamo"] = ["sesamo", "tahini"],
        ["Mostaza"] = ["mostaza", "mustard"],
    };

    private readonly NidoDbContext _db;

    public RecetaRepository(NidoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RecetaResult>> GetAllAsync(Guid hogarId, CancellationToken ct)
    {
        var recetas = await _db.Recetas
            .AsNoTracking()
            .Include(receta => receta.IngredientesReceta)
                .ThenInclude(ingrediente => ingrediente.Producto)
            .Include(receta => receta.InfoNutricionalReceta)
            .Include(receta => receta.PasosReceta)
            .Include(receta => receta.RecetaElectrodomesticos)
            .OrderBy(receta => receta.Nombre)
            .ToListAsync(ct);

        var productosEnStock = await GetProductosEnStockAsync(hogarId, ct);
        var vecesCocinadas = await GetVecesCocinadadasAsync(hogarId, ct);

        return recetas.Select(receta =>
            ToResult(receta, productosEnStock, vecesCocinadas.GetValueOrDefault(receta.Id, 0))).ToList();
    }

    public async Task<RecetaResult?> GetByIdAsync(Guid id, Guid hogarId, CancellationToken ct)
    {
        var receta = await _db.Recetas
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Include(receta => receta.IngredientesReceta)
                .ThenInclude(ingrediente => ingrediente.Producto)
            .Include(receta => receta.InfoNutricionalReceta)
            .Include(receta => receta.PasosReceta)
            .Include(receta => receta.RecetaElectrodomesticos)
            .FirstOrDefaultAsync(ct);

        if (receta is null)
            return null;

        var productosEnStock = await GetProductosEnStockAsync(hogarId, ct);
        var vecesCocinada = await _db.RecetasCocinadas
            .AsNoTracking()
            .CountAsync(rc => rc.RecetaId == id && rc.HogarId == hogarId, ct);

        return ToResult(receta, productosEnStock, vecesCocinada);
    }

    public async Task<CocinarRecetaResult?> CocinarAsync(CocinarRecetaCommand command, CancellationToken ct)
    {
        var receta = await _db.Recetas
            .AsNoTracking()
            .Where(r => r.Id == command.RecetaId)
            .Include(r => r.IngredientesReceta)
            .FirstOrDefaultAsync(ct);

        if (receta is null)
            return null;

        var ingredientesConProducto = receta.IngredientesReceta
            .Where(i => i.ProductoId.HasValue && i.Cantidad.HasValue && i.Cantidad.Value > 0)
            .ToList();

        foreach (var ingrediente in ingredientesConProducto)
        {
            await ReducirStockAsync(
                command.HogarId,
                ingrediente.ProductoId!.Value,
                ingrediente.Cantidad!.Value,
                command.UsuarioId,
                ct);
        }

        var registro = new RecetasCocinada
        {
            Id = Guid.NewGuid(),
            RecetaId = command.RecetaId,
            HogarId = command.HogarId,
            CocinadoPor = command.UsuarioId,
            Fecha = DateTime.UtcNow,
            PorcionesCocinadas = receta.Porciones
        };

        _db.RecetasCocinadas.Add(registro);
        await _db.SaveChangesAsync(ct);

        var vecesCocinada = await _db.RecetasCocinadas
            .CountAsync(rc => rc.RecetaId == command.RecetaId && rc.HogarId == command.HogarId, ct);

        return new CocinarRecetaResult(command.RecetaId, vecesCocinada);
    }

    private async Task ReducirStockAsync(Guid hogarId, Guid productoId, decimal cantidad, Guid usuarioId, CancellationToken ct)
    {
        var stockItems = await _db.StockHogars
            .Where(s => s.HogarId == hogarId
                     && s.ProductoId == productoId
                     && (s.CantidadActual == null || s.CantidadActual > 0))
            .OrderBy(s => s.FechaVencimiento ?? DateOnly.MaxValue)
            .ThenBy(s => s.CreatedAt)
            .ToListAsync(ct);

        var restante = cantidad;
        foreach (var item in stockItems)
        {
            if (restante <= 0) break;

            var disponible = item.CantidadActual ?? 0;

            if (disponible <= restante)
            {
                restante -= disponible;
                _db.StockHogars.Remove(item);
            }
            else
            {
                item.CantidadActual = disponible - restante;
                item.UpdatedBy = usuarioId;
                item.UpdatedAt = DateTime.UtcNow;
                restante = 0;
            }
        }
    }

    private static RecetaResult ToResult(Receta receta, IReadOnlySet<Guid> productosEnStock, int vecesCocinada)
    {
        var nutricion = receta.InfoNutricionalReceta.FirstOrDefault();

        return new RecetaResult(
            receta.Id,
            receta.Nombre,
            receta.Descripcion,
            receta.TiempoCoccionMin,
            receta.Dificultad,
            receta.Porciones,
            receta.FuenteId,
            receta.ImagenUrl,
            nutricion?.Calorias,
            nutricion?.Proteinas,
            nutricion?.Carbohidratos,
            nutricion?.Grasas,
            receta.IngredientesReceta
                .OrderBy(ingrediente => ingrediente.NombreIngrediente)
                .Select(ingrediente => new RecetaIngredienteResult(
                    ingrediente.Id,
                    ingrediente.ProductoId,
                    ingrediente.NombreIngrediente,
                    ingrediente.Producto != null ? ingrediente.Producto.Nombre : null,
                    ingrediente.Cantidad,
                    ingrediente.Unidad,
                    ingrediente.ProductoId.HasValue && productosEnStock.Contains(ingrediente.ProductoId.Value),
                    DetectAlergenos(ingrediente)))
                .ToList(),
            receta.PasosReceta
                .OrderBy(paso => paso.Orden)
                .Select(paso => new RecetaPasoResult(
                    paso.Id,
                    paso.Orden,
                    paso.Descripcion))
                .ToList(),
            receta.RecetaElectrodomesticos
                .OrderBy(electrodomestico => electrodomestico.TipoRequerido)
                .Select(electrodomestico => new RecetaElectrodomesticoResult(
                    electrodomestico.Id,
                    electrodomestico.TipoRequerido))
                .ToList(),
            vecesCocinada);
    }

    private static IReadOnlyList<string> DetectAlergenos(IngredientesRecetum ingrediente)
    {
        var text = Normalize($"{ingrediente.NombreIngrediente} {ingrediente.Producto?.Nombre}");
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return AllergenAliases
            .Where(entry => entry.Value.Any(alias => text.Contains(Normalize(alias), StringComparison.Ordinal)))
            .Select(entry => entry.Key)
            .ToList();
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private async Task<IReadOnlySet<Guid>> GetProductosEnStockAsync(Guid hogarId, CancellationToken ct)
    {
        if (hogarId == Guid.Empty)
            return new HashSet<Guid>();

        var productoIds = await _db.StockHogars
            .AsNoTracking()
            .Where(stock => stock.HogarId == hogarId && (stock.CantidadActual == null || stock.CantidadActual > 0))
            .Select(stock => stock.ProductoId)
            .Distinct()
            .ToListAsync(ct);

        return productoIds.ToHashSet();
    }

    private async Task<IReadOnlyDictionary<Guid, int>> GetVecesCocinadadasAsync(Guid hogarId, CancellationToken ct)
    {
        if (hogarId == Guid.Empty)
            return new Dictionary<Guid, int>();

        return await _db.RecetasCocinadas
            .AsNoTracking()
            .Where(rc => rc.HogarId == hogarId)
            .GroupBy(rc => rc.RecetaId)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), ct);
    }
}
