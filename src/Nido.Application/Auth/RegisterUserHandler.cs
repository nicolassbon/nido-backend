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
        if (await _repository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var passwordHash = _passwordHasher.Hash(command.Password);
        var (usuarioId, hogarId) = await _repository.CreateUserWithDefaultHouseholdAsync(
            command.Nombre.Trim(),
            normalizedEmail,
            passwordHash,
            command.Sexo.Trim(),
            command.FotoUrl,
            cancellationToken);

        var token = _jwtTokenService.CreateToken(usuarioId, hogarId, normalizedEmail);
        return new RegisterUserResult(usuarioId, hogarId, token);
    }
}
