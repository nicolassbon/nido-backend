using Nido.Application.Payments;

namespace Nido.Application.Alacena;

public sealed class ScanNutritionInfoHandler
{
    private readonly INutritionInfoRepository _nutritionRepository;
    private readonly INutritionLabelParser _nutritionLabelParser;
    private readonly IEntitlementService _entitlementService;

    public ScanNutritionInfoHandler(
        INutritionInfoRepository nutritionRepository,
        INutritionLabelParser nutritionLabelParser,
        IEntitlementService entitlementService)
    {
        _nutritionRepository = nutritionRepository;
        _nutritionLabelParser = nutritionLabelParser;
        _entitlementService = entitlementService;
    }

    public async Task<NutritionInfoResult?> Handle(ScanNutritionInfoCommand command, CancellationToken ct)
    {
        await _entitlementService.EnsurePremiumAsync(command.HogarId, ct);

        var exists = await _nutritionRepository.StockBelongsToHogarAsync(command.StockId, command.HogarId, ct);
        if (!exists)
        {
            return null;
        }

        return await _nutritionLabelParser.ParseAsync(command.Image, ct);
    }
}
