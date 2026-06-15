namespace Nido.Application.Common.Images;

public class ImageUploadException : Exception
{
    protected ImageUploadException(string message) : base(message)
    {
    }
}

public sealed class MissingImageFileException : ImageUploadException
{
    public MissingImageFileException() : base("No se envió ninguna imagen.")
    {
    }
}

public sealed class UnsupportedImageTypeException : ImageUploadException
{
    public UnsupportedImageTypeException() : base("Formato de imagen no soportado. Use JPG, PNG o WebP.")
    {
    }
}

public sealed class ImageSizeExceededException : ImageUploadException
{
    public ImageSizeExceededException() : base("La imagen excede el tamaño máximo permitido.")
    {
    }
}

public sealed class ImageStorageFailureException : ImageUploadException
{
    public ImageStorageFailureException(Exception? innerException = null)
        : base("Error al guardar la imagen. Intente nuevamente.")
    {
        if (innerException is not null)
        {
            HResult = innerException.HResult;
        }
    }
}

public sealed class ImageStorageConfigurationException : ImageUploadException
{
    public ImageStorageConfigurationException() : base("El servicio de imágenes no está configurado.")
    {
    }
}
