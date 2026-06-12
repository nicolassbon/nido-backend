using Nido.Application.Common.Assets;
using Nido.Application.Common.Images;
using Nido.Application.Common.Storage;

namespace Nido.Application.Electrodomesticos.UploadElectrodomesticoImage;

public sealed class UploadElectrodomesticoImageHandler
{
    private readonly IElectrodomesticoImageRepository _repository;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly IFileStorageService _storageService;
    private readonly StorageKeyFactory _storageKeyFactory;
    private readonly IPublicAssetUrlResolver _publicAssetUrlResolver;

    public UploadElectrodomesticoImageHandler(IElectrodomesticoImageRepository repository, IImageProcessingService imageProcessingService, IFileStorageService storageService, StorageKeyFactory storageKeyFactory, IPublicAssetUrlResolver publicAssetUrlResolver)
    {
        _repository = repository;
        _imageProcessingService = imageProcessingService;
        _storageService = storageService;
        _storageKeyFactory = storageKeyFactory;
        _publicAssetUrlResolver = publicAssetUrlResolver;
    }

    public async Task<UploadElectrodomesticoImageResult> Handle(UploadElectrodomesticoImageCommand command, CancellationToken cancellationToken)
    {
        var target = await _repository.GetImageTargetAsync(command.ElectrodomesticoId, command.HogarId, cancellationToken)
            ?? throw new ElectrodomesticoImageTargetNotFoundException();

        var processed = await _imageProcessingService.ProcessAsync(command.Image, cancellationToken);
        var key = _storageKeyFactory.ForElectrodomestico();

        await using var stream = processed.OpenReadStream();
        await _storageService.UploadAsync(stream, key, processed.ContentType, cancellationToken);

        await _repository.UpdateImageKeyAsync(command.ElectrodomesticoId, command.HogarId, key, cancellationToken);

        if (StorageKeyRules.IsStorageKey(target.CurrentStorageKey))
        {
            await _storageService.DeleteAsync(target.CurrentStorageKey!, cancellationToken);
        }

        return new UploadElectrodomesticoImageResult(_publicAssetUrlResolver.Resolve(key) ?? key);
    }
}
