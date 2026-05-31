namespace Nido.Application.Auth.Register;

public sealed record RegisterUserCommand(
    string Nombre,
    string Email,
    string Password,
    string Sexo,
    RegistrationProfileImageUpload? Foto);
