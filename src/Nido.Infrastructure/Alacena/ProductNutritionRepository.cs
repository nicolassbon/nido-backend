using Microsoft.EntityFrameworkCore;
using Nido.Application.Alacena;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Alacena;

public sealed class ProductNutritionRepository : INutritionInfoRepository
{
    private readonly NidoDbContext _db;

    public ProductNutritionRepository(NidoDbContext db)
    {
        _db = db;
    }

    public Task<bool> StockBelongsToHogarAsync(Guid stockId, Guid hogarId, CancellationToken ct)
    {
        return _db.StockHogars
            .AsNoTracking()
            .AnyAsync(stock => stock.Id == stockId && stock.HogarId == hogarId, ct);
    }

    public async Task<NutritionInfoResult?> SaveForStockAsync(
        Guid stockId,
        Guid hogarId,
        SaveNutritionInfoRequestModel request,
        CancellationToken ct)
    {
        var stock = await _db.StockHogars
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == stockId && item.HogarId == hogarId, ct);

        if (stock is null)
        {
            return null;
        }

        var info = await _db.InfoNutricionalProductos
            .Include(item => item.Detalles)
            .FirstOrDefaultAsync(item => item.ProductoId == stock.ProductoId, ct);

        if (info is null)
        {
            info = new InfoNutricionalProducto
            {
                Id = Guid.NewGuid(),
                ProductoId = stock.ProductoId
            };
            _db.InfoNutricionalProductos.Add(info);
        }

        info.Calorias = request.Calorias;
        info.Proteinas = request.Proteinas;
        info.Carbohidratos = request.Carbohidratos;
        info.Grasas = request.Grasas;
        info.Porcion = NormalizeText(request.Porcion, 100);
        info.Base = NormalizeText(request.Base, 100);

        _db.InfoNutricionalProductoDetalles.RemoveRange(info.Detalles);
        info.Detalles.Clear();

        foreach (var item in request.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.Nombre))
            .OrderBy(item => item.Orden)
            .Select((item, index) => new { item, index }))
        {
            info.Detalles.Add(new InfoNutricionalProductoDetalle
            {
                Id = Guid.NewGuid(),
                InfoNutricionalProductoId = info.Id,
                Nombre = NormalizeText(item.item.Nombre, 150) ?? string.Empty,
                Valor = item.item.Valor,
                Unidad = NormalizeText(item.item.Unidad, 30),
                PorcentajeDiario = item.item.PorcentajeDiario,
                Orden = item.item.Orden > 0 ? item.item.Orden : item.index + 1
            });
        }

        await _db.SaveChangesAsync(ct);

        return new NutritionInfoResult(
            info.Calorias,
            info.Proteinas,
            info.Carbohidratos,
            info.Grasas,
            info.Porcion,
            info.Base,
            info.Detalles
                .OrderBy(item => item.Orden)
                .Select(item => new NutritionInfoItemResult(
                    item.Nombre,
                    item.Valor,
                    item.Unidad,
                    item.PorcentajeDiario,
                    item.Orden))
                .ToArray());
    }

    private static string? NormalizeText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}

