using Nido.Application.Common.Images;

namespace Nido.Application.Electrodomesticos.UploadElectrodomesticoImage;

public sealed record UploadElectrodomesticoImageCommand(Guid ElectrodomesticoId, Guid HogarId, ImageUpload Image);
public sealed record UploadElectrodomesticoImageResult(string ImagenUrl);
public sealed record ElectrodomesticoImageTarget(Guid Id, string? CurrentStorageKey);

public interface IElectrodomesticoImageRepository
{
    Task<ElectrodomesticoImageTarget?> GetImageTargetAsync(Guid electrodomesticoId, Guid hogarId, CancellationToken cancellationToken);
    Task UpdateImageKeyAsync(Guid electrodomesticoId, Guid hogarId, string storageKey, CancellationToken cancellationToken);
}

public sealed class ElectrodomesticoImageTargetNotFoundException : Exception
{
    public ElectrodomesticoImageTargetNotFoundException() : base("Electrodoméstico no encontrado.")
    {
    }
}
