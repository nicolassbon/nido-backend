namespace Nido.Application.Common.ProfileImages;

public sealed record UserProfileImageMetadata(
    string StorageKey,
    string ContentType,
    int Width,
    int Height,
    long Length);
