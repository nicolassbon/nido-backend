namespace Nido.Application.Alacena;

public sealed class ScanNutritionInfoHandler
{
    private readonly INutritionInfoRepository _nutritionRepository;
    private readonly INutritionLabelParser _nutritionLabelParser;

    public ScanNutritionInfoHandler(
        INutritionInfoRepository nutritionRepository,
        INutritionLabelParser nutritionLabelParser)
    {
        _nutritionRepository = nutritionRepository;
        _nutritionLabelParser = nutritionLabelParser;
    }

    public async Task<NutritionInfoResult?> Handle(ScanNutritionInfoCommand command, CancellationToken ct)
    {
        var exists = await _nutritionRepository.StockBelongsToHogarAsync(command.StockId, command.HogarId, ct);
        if (!exists)
        {
            return null;
        }

        return await _nutritionLabelParser.ParseAsync(command.Image, ct);
    }
}

