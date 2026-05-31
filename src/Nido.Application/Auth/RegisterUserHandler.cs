using Microsoft.Extensions.Logging;
using Nido.Application.Common.ProfileImages;

namespace Nido.Application.Auth;

public sealed class RegisterUserHandler
{
    private const string PasswordComplexityPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d@$!%*?&.]{8,}$";

    private readonly IAuthRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IProfileImageProcessor _profileImageProcessor;
    private readonly IProfileImageStorage _profileImageStorage;
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(
        IAuthRepository repository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IProfileImageProcessor profileImageProcessor,
        IProfileImageStorage profileImageStorage,
        ILogger<RegisterUserHandler> logger)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _profileImageProcessor = profileImageProcessor;
        _profileImageStorage = profileImageStorage;
        _logger = logger;
    }

    public async Task<RegisterUserResult> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        ValidateCommand(command);

        var normalizedEmail = EmailNormalizer.Normalize(command.Email);
        var existingUser = await _repository.FindByEmailAsync(normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            return await HandleExistingUserAsync(existingUser, command.Password, normalizedEmail, cancellationToken);
        }

        var usuarioId = Guid.NewGuid();
        var registration = BuildNewRegistrationData(command, normalizedEmail, usuarioId);
        var imageUpload = await UploadProfileImageIfPresentAsync(command.Foto, usuarioId, cancellationToken);

        try
        {
            await PersistNewUserAsync(registration, imageUpload?.Metadata, cancellationToken);
        }
        catch
        {
            await CleanupUploadedProfileImageAsync(imageUpload?.StorageKey, registration.UsuarioId, registration.HogarId, normalizedEmail);
            throw;
        }

        var (accessToken, refreshToken) = await AuthTokenHelper.CreateAndPersistRefreshTokenAsync(
            _jwtTokenService,
            _repository,
            registration.UsuarioId,
            registration.HogarId,
            normalizedEmail,
            command.Nombre,
            cancellationToken);

        return new RegisterUserResult(registration.UsuarioId, registration.HogarId, accessToken, refreshToken);
    }

    private static void ValidateCommand(RegisterUserCommand command)
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

        if (!System.Text.RegularExpressions.Regex.IsMatch(command.Password, PasswordComplexityPattern))
        {
            throw new ArgumentException("Password does not meet complexity requirements.");
        }
    }

    private async Task<RegisterUserResult> HandleExistingUserAsync(
        User existingUser,
        string password,
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        if (existingUser.OauthProvider != "google" || !string.IsNullOrEmpty(existingUser.PasswordHash))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var passwordHash = _passwordHasher.Hash(password);
        var updatedUser = existingUser with { PasswordHash = passwordHash };
        await _repository.UpdateUserAsync(updatedUser, cancellationToken);

        var hogarId = await _repository.GetUserHogarIdAsync(existingUser.Id, cancellationToken)
            ?? throw new InvalidOperationException("User has no associated household.");

        var (accessToken, refreshToken) = await AuthTokenHelper.CreateAndPersistRefreshTokenAsync(
            _jwtTokenService,
            _repository,
            existingUser.Id,
            hogarId,
            normalizedEmail,
            existingUser.Nombre,
            cancellationToken);

        return new RegisterUserResult(existingUser.Id, hogarId, accessToken, refreshToken);
    }

    private NewUserRegistrationData BuildNewRegistrationData(RegisterUserCommand command, string normalizedEmail, Guid usuarioId)
    {
        return new NewUserRegistrationData(
            usuarioId,
            Guid.NewGuid(),
            command.Nombre.Trim(),
            normalizedEmail,
            _passwordHasher.Hash(command.Password),
            command.Sexo.Trim());
    }

    private async Task<ProfileImageUploadResult?> UploadProfileImageIfPresentAsync(
        RegistrationProfileImageUpload? foto,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        if (foto is null)
        {
            return null;
        }

        var processed = await _profileImageProcessor.ProcessAsync(foto, cancellationToken);
        var storageKey = $"usuarios/{usuarioId}/profile/{Guid.NewGuid():N}.webp";
        await _profileImageStorage.UploadAsync(storageKey, processed.Content, processed.ContentType, cancellationToken);

        var metadata = new UserProfileImageMetadata(
            storageKey,
            processed.ContentType,
            processed.Width,
            processed.Height,
            processed.Length);

        return new ProfileImageUploadResult(storageKey, metadata);
    }

    private Task PersistNewUserAsync(
        NewUserRegistrationData registration,
        UserProfileImageMetadata? imageMetadata,
        CancellationToken cancellationToken)
    {
        return _repository.CreateUserWithPasswordAsync(
            registration.UsuarioId,
            registration.HogarId,
            registration.Nombre,
            registration.Email,
            registration.PasswordHash,
            registration.Sexo,
            imageMetadata,
            cancellationToken);
    }

    private async Task CleanupUploadedProfileImageAsync(
        string? uploadedStorageKey,
        Guid usuarioId,
        Guid hogarId,
        string normalizedEmail)
    {
        if (uploadedStorageKey is null)
        {
            return;
        }

        try
        {
            await _profileImageStorage.DeleteAsync(uploadedStorageKey, CancellationToken.None);
        }
        catch (Exception cleanupEx)
        {
            _logger.LogError(
                cleanupEx,
                "Profile image cleanup failed for usuarioId {UsuarioId}, hogarId {HogarId}, email {Email}, storageKey {StorageKey}",
                usuarioId,
                hogarId,
                normalizedEmail,
                uploadedStorageKey);
        }
    }

    private sealed record NewUserRegistrationData(
        Guid UsuarioId,
        Guid HogarId,
        string Nombre,
        string Email,
        string PasswordHash,
        string Sexo);

    private sealed record ProfileImageUploadResult(
        string StorageKey,
        UserProfileImageMetadata Metadata);
}
