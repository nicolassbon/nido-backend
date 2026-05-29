namespace Nido.Application.Auth;

public sealed class RegisterUserHandler
{
    private readonly IAuthRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public RegisterUserHandler(IAuthRepository repository, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<RegisterUserResult> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var missingFields = new List<string>();
        if (string.IsNullOrWhiteSpace(command.Nombre)) missingFields.Add(nameof(command.Nombre));
        if (string.IsNullOrWhiteSpace(command.Email)) missingFields.Add(nameof(command.Email));
        if (string.IsNullOrWhiteSpace(command.Password)) missingFields.Add(nameof(command.Password));
        if (string.IsNullOrWhiteSpace(command.Sexo)) missingFields.Add(nameof(command.Sexo));

        if (missingFields.Any())
        {
            throw new ArgumentException($"Missing required fields: {string.Join(", ", missingFields)}");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(command.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d@$!%*?&.]{8,}$"))
        {
            throw new ArgumentException("Password does not meet complexity requirements.");
        }

        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var existingUser = await _repository.FindByEmailAsync(normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            if (existingUser.OauthProvider == "google" && string.IsNullOrEmpty(existingUser.PasswordHash))
            {
                var passwordHash = _passwordHasher.Hash(command.Password);
                var updatedUser = existingUser with { PasswordHash = passwordHash };
                await _repository.UpdateUserAsync(updatedUser, cancellationToken);

                var hogarId = await _repository.GetUserHogarIdAsync(existingUser.Id, cancellationToken)
                    ?? throw new InvalidOperationException("User has no associated household.");

                var (accessToken, refreshToken) = _jwtTokenService.CreateAuthTokens(existingUser.Id, hogarId, normalizedEmail);
                var refreshTokenHash = _jwtTokenService.HashRefreshToken(refreshToken);
                var expiresAt = DateTime.UtcNow.AddDays(7);

                await _repository.AddRefreshTokenAsync(existingUser.Id, refreshTokenHash, expiresAt, cancellationToken);

                return new RegisterUserResult(existingUser.Id, hogarId, accessToken, refreshToken);
            }

            throw new InvalidOperationException("Email already exists.");
        }

        var newHash = _passwordHasher.Hash(command.Password);
        var (usuarioId, newHogarId) = await _repository.CreateUserWithDefaultHouseholdAsync(
            command.Nombre.Trim(),
            normalizedEmail,
            newHash,
            command.Sexo.Trim(),
            command.FotoUrl,
            cancellationToken);

        var token = _jwtTokenService.CreateToken(usuarioId, newHogarId, normalizedEmail);
        return new RegisterUserResult(usuarioId, newHogarId, token);
    }
}
