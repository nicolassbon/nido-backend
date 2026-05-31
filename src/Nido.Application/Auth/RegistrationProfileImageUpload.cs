namespace Nido.Application.Auth;

public sealed record RegistrationProfileImageUpload(
    string FileName,
    string ContentType,
    byte[] Content);
