namespace Nido.Api.Contracts.Auth;

public sealed record AddPasswordRequest(string NewPassword, string NewPasswordConfirmation);
