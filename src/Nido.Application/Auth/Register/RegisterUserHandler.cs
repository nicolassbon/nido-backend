using Microsoft.Extensions.Logging;
using Nido.Application.Auth;
using Nido.Application.Auth.Exceptions;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Common.Notifications;
using Nido.Application.Common.ProfileImages;
using Nido.Application.Common.Storage;

namespace Nido.Application.Auth.Register;

public sealed class RegisterUserHandler
{
    private readonly IAuthRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IProfileImageProcessor _profileImageProcessor;
    private readonly IFileStorageService _fileStorageService;
    private readonly StorageKeyFactory _storageKeyFactory;
    private readonly IEmailService _emailService;
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(
        IAuthRepository repository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IProfileImageProcessor profileImageProcessor,
        IFileStorageService fileStorageService,
        StorageKeyFactory storageKeyFactory,
        IEmailService emailService,
        ILogger<RegisterUserHandler> logger)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _profileImageProcessor = profileImageProcessor;
        _fileStorageService = fileStorageService;
        _storageKeyFactory = storageKeyFactory;
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
        var fotoStorageKey = await UploadProfileImageIfPresentAsync(command.Foto, usuarioId, cancellationToken);

        try
        {
            await PersistNewUserAsync(registration, fotoStorageKey, cancellationToken);
        }
        catch (EmailAlreadyExistsException)
        {
            await CleanupUploadedProfileImageAsync(fotoStorageKey, usuarioId, registration.HogarId);
            await TrySendDuplicateSignupNoticeAsync(normalizedEmail, cancellationToken);
            return RegisterUserResult.SilentSuccess();
        }
        catch
        {
            await CleanupUploadedProfileImageAsync(fotoStorageKey, usuarioId, registration.HogarId);
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

    private async Task<string?> UploadProfileImageIfPresentAsync(
        RegistrationProfileImageUpload? foto,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        if (foto is null)
        {
            return null;
        }

        var processed = await _profileImageProcessor.ProcessAsync(foto, cancellationToken);
        var storageKey = _storageKeyFactory.ForAvatar(usuarioId);

        await using var stream = new MemoryStream(processed.Content);
        await _fileStorageService.UploadAsync(stream, storageKey, processed.ContentType, cancellationToken);

        return storageKey;
    }

    private Task PersistNewUserAsync(
        NewUserRegistrationData registration,
        string? fotoStorageKey,
        CancellationToken cancellationToken)
    {
        return _repository.CreateUserWithPasswordAsync(
            registration.UsuarioId,
            registration.HogarId,
            registration.Nombre,
            registration.Email,
            registration.PasswordHash,
            registration.Sexo,
            fotoStorageKey,
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
        Guid hogarId)
    {
        if (uploadedStorageKey is null)
        {
            return;
        }

        try
        {
            await _fileStorageService.DeleteAsync(uploadedStorageKey, CancellationToken.None);
        }
        catch (Exception cleanupEx)
        {
            _logger.LogError(
                cleanupEx,
                "Profile image cleanup failed for usuarioId {UsuarioId}, hogarId {HogarId}, storageKey {StorageKey}",
                usuarioId,
                hogarId,
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
}
