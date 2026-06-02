namespace Nido.Application.Auth.Register;

public sealed record RegistrationProfileImageUpload(
    string FileName,
    string ContentType,
    byte[] Content);
