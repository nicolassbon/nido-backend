using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Nido.Application.Common.Images;
using Nido.Application.Common.Storage;

namespace Nido.Infrastructure.Storage;

public sealed class SpacesS3Storage : IFileStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly SpacesOptions _options;

    public SpacesS3Storage(IAmazonS3 s3, IOptions<SpacesOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public async Task<FileStorageUploadResult> UploadAsync(Stream stream, string key, string contentType, CancellationToken cancellationToken)
    {
        try
        {
            EnsureConfigured();
            await _s3.PutObjectAsync(CreatePutObjectRequest(_options, stream, key, contentType), cancellationToken);
            return new FileStorageUploadResult(key, BuildPublicUrl(_options.PublicBaseUrl, key));
        }
        catch (ImageStorageConfigurationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ImageStorageFailureException(ex);
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key) || !StorageKeyRules.IsStorageKey(key))
        {
            return;
        }

        try
        {
            EnsureConfigured();
            await _s3.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _options.Bucket,
                Key = key
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new ImageStorageFailureException(ex);
        }
    }

    internal static PutObjectRequest CreatePutObjectRequest(SpacesOptions options, Stream stream, string key, string contentType)
        => new()
        {
            BucketName = options.Bucket,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead,
            Headers = { ContentLength = stream.Length }
        };

    internal static string BuildPublicUrl(string publicBaseUrl, string key)
        => $"{publicBaseUrl.TrimEnd('/')}/{key}";

    private void EnsureConfigured()
    {
        if (!_options.HasUploadConfiguration())
        {
            throw new ImageStorageConfigurationException();
        }
    }
}
