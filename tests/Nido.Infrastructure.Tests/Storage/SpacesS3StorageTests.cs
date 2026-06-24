using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Nido.Infrastructure.Storage;

namespace Nido.Infrastructure.Tests.Storage;

public sealed class SpacesS3StorageTests
{
    [Fact]
    public void CreatePutObjectRequest_BuildsPublicReadRequestWithBucketKeyAndContentType()
    {
        var options = new SpacesOptions
        {
            Bucket = "nido-dev",
            PublicBaseUrl = "https://nido-dev.nyc3.digitaloceanspaces.com"
        };
        using var stream = new MemoryStream([1, 2, 3]);

        var request = SpacesS3Storage.CreatePutObjectRequest(
            options,
            stream,
            "products/image.webp",
            "image/webp");

        Assert.Equal("nido-dev", request.BucketName);
        Assert.Equal("products/image.webp", request.Key);
        Assert.Equal("image/webp", request.ContentType);
        Assert.Equal(S3CannedACL.PublicRead, request.CannedACL);
        Assert.Equal(stream.Length, request.Headers.ContentLength);
        Assert.Same(stream, request.InputStream);
    }

    [Fact]
    public async Task UploadAsync_WhenRequiredConfigurationIsMissing_ThrowsSafeConfigurationException()
    {
        var options = Options.Create(new SpacesOptions());
        var storage = new SpacesS3Storage(new Amazon.S3.AmazonS3Client("placeholder", "placeholder", Amazon.RegionEndpoint.USEast1), options);
        await using var stream = new MemoryStream([1, 2, 3]);

        var exception = await Assert.ThrowsAsync<Nido.Application.Common.Images.ImageStorageConfigurationException>(() =>
            storage.UploadAsync(stream, "products/image.webp", "image/webp", CancellationToken.None));

        Assert.Equal("El servicio de imágenes no está configurado.", exception.Message);
    }
}
