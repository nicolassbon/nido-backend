using Microsoft.Extensions.Logging;
using Nido.Application.Auth;
using Nido.Application.Auth.Exceptions;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Common.Notifications;
using Nido.Application.Common.ProfileImages;

namespace Nido.Application.Auth.Register;

public sealed class RegisterUserHandler
{
    private readonly IAuthRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IProfileImageProcessor _profileImageProcessor;
    private readonly IProfileImageStorage _profileImageStorage;
    private readonly IEmailService _emailService;
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(
        IAuthRepository repository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IProfileImageProcessor profileImageProcessor,
        IProfileImageStorage profileImageStorage,
        IEmailService emailService,
        ILogger<RegisterUserHandler> logger)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _profileImageProcessor = profileImageProcessor;
        _profileImageStorage = profileImageStorage;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<RegisterUserResult> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        ValidateCommand(command);

        var normalizedEmail = EmailNormalizer.Normalize(command.Email);
        var existingUser = await _repository.FindByEmailAsync(normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            await TrySendDuplicateSignupNoticeAsync(normalizedEmail, cancellationToken);
            return RegisterUserResult.SilentSuccess();
        }

        var usuarioId = Guid.NewGuid();
        var registration = BuildNewRegistrationData(command, normalizedEmail, usuarioId);
        var imageUpload = await UploadProfileImageIfPresentAsync(command.Foto, usuarioId, cancellationToken);

        try
        {
            await PersistNewUserAsync(registration, imageUpload?.Metadata, cancellationToken);
        }
        catch (EmailAlreadyExistsException)
        {
            await CleanupUploadedProfileImageAsync(imageUpload?.StorageKey, registration.UsuarioId, registration.HogarId, normalizedEmail);
            await TrySendDuplicateSignupNoticeAsync(normalizedEmail, cancellationToken);
            return RegisterUserResult.SilentSuccess();
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

        return RegisterUserResult.Created(registration.UsuarioId, registration.HogarId, accessToken, refreshToken);
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
            throw new MissingRegistrationFieldsException(missingFields);
        }

        if (!PasswordRules.IsValid(command.Password))
        {
            throw new WeakPasswordException();
        }
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

    private async Task TrySendDuplicateSignupNoticeAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendDuplicateSignupNoticeEmailAsync(normalizedEmail, cancellationToken);
        }
        catch (Exception emailEx)
        {
            _logger.LogError(emailEx, "Duplicate signup notice email failed for {Email}", normalizedEmail);
        }
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
