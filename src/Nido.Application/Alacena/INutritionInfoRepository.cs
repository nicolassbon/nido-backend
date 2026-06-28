namespace Nido.Application.Alacena;

public interface INutritionInfoRepository
{
    Task<bool> StockBelongsToHogarAsync(Guid stockId, Guid hogarId, CancellationToken ct);
    Task<NutritionInfoResult?> SaveForStockAsync(Guid stockId, Guid hogarId, SaveNutritionInfoRequestModel request, CancellationToken ct);
}

