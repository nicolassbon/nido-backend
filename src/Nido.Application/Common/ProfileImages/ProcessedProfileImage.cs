namespace Nido.Application.Common.ProfileImages;

public sealed record ProcessedProfileImage(
    byte[] Content,
    string ContentType,
    int Width,
    int Height,
    long Length);
