namespace Nido.Application.Auth;

public sealed record RegisterUserCommand(
    string Nombre,
    string Email,
    string Password,
    string Sexo,
    RegistrationProfileImageUpload? Foto);
