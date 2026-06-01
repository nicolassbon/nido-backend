using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nido.Api.Contracts.Auth;
using Nido.Application.Auth.Google.Link;
using Nido.Application.Auth.Google.Login;
using Nido.Application.Auth.ChangePassword;
using Nido.Application.Auth.AddPassword;
using Nido.Application.Auth.ForgotPassword;
using Nido.Application.Auth.Login;
using Nido.Application.Auth.Logout;
using Nido.Application.Auth.ResetPassword;
using Nido.Application.Auth.RefreshToken;
using Nido.Application.Auth.Register;
using Nido.Application.Common.ProfileImages;
using Nido.Application.Common.Security;
using Nido.Infrastructure.Auth;
using Nido.Infrastructure.ProfileImages;

namespace Nido.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterUserHandler _registerUserHandler;
    private readonly LoginHandler _loginHandler;
    private readonly GoogleLoginHandler _googleLoginHandler;
    private readonly RefreshTokenHandler _refreshTokenHandler;
    private readonly LogoutHandler _logoutHandler;
    private readonly LinkGoogleHandler _linkGoogleHandler;
    private readonly ForgotPasswordHandler _forgotPasswordHandler;
    private readonly ResetPasswordHandler _resetPasswordHandler;
    private readonly ChangePasswordHandler _changePasswordHandler;
    private readonly AddPasswordHandler _addPasswordHandler;
    private readonly ICurrentUserContext _currentUser;
    private readonly IOptions<ProfileImageOptions> _profileImageOptions;
    private readonly IOptions<JwtOptions> _jwtOptions;

    public AuthController(
        RegisterUserHandler registerUserHandler,
        LoginHandler loginHandler,
        GoogleLoginHandler googleLoginHandler,
        RefreshTokenHandler refreshTokenHandler,
        LogoutHandler logoutHandler,
        LinkGoogleHandler linkGoogleHandler,
        ForgotPasswordHandler forgotPasswordHandler,
        ResetPasswordHandler resetPasswordHandler,
        ChangePasswordHandler changePasswordHandler,
        AddPasswordHandler addPasswordHandler,
        ICurrentUserContext currentUser,
        IOptions<ProfileImageOptions> profileImageOptions,
        IOptions<JwtOptions> jwtOptions)
    {
        _registerUserHandler = registerUserHandler;
        _loginHandler = loginHandler;
        _googleLoginHandler = googleLoginHandler;
        _refreshTokenHandler = refreshTokenHandler;
        _logoutHandler = logoutHandler;
        _linkGoogleHandler = linkGoogleHandler;
        _forgotPasswordHandler = forgotPasswordHandler;
        _resetPasswordHandler = resetPasswordHandler;
        _changePasswordHandler = changePasswordHandler;
        _addPasswordHandler = addPasswordHandler;
        _currentUser = currentUser;
        _profileImageOptions = profileImageOptions;
        _jwtOptions = jwtOptions;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Register([FromForm] RegisterRequest request, CancellationToken cancellationToken)
    {
        RegistrationProfileImageUpload? foto = null;
        if (request.Foto is not null)
        {
            if (request.Foto.Length == 0)
            {
                throw new ArgumentException("Empty file is not allowed for profile image.");
            }

            var maxSizeInBytes = _profileImageOptions.Value.MaxBytes;
            if (request.Foto.Length > maxSizeInBytes)
            {
                throw new ArgumentException("Profile image exceeds the allowed limit.");
            }

            await using var memoryStream = new MemoryStream();
            await request.Foto.CopyToAsync(memoryStream, cancellationToken);

            foto = new RegistrationProfileImageUpload(
                request.Foto.FileName,
                request.Foto.ContentType,
                memoryStream.ToArray());
        }

        var result = await _registerUserHandler.Handle(
            new RegisterUserCommand(request.Nombre, request.Email, request.Password, request.Sexo, foto),
            cancellationToken);

        if (result.RefreshToken is not null)
        {
            SetRefreshTokenCookie(result.RefreshToken);
        }

        return StatusCode(StatusCodes.Status201Created, new RegisterResponse(result.UsuarioId, result.HogarId, result.AccessToken));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _loginHandler.Handle(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(new LoginResponse(result.UsuarioId, result.HogarId, result.AccessToken));
    }

    [AllowAnonymous]
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _googleLoginHandler.Handle(
            new GoogleLoginCommand(request.IdToken),
            cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(new GoogleLoginResponse(result.UsuarioId, result.HogarId, result.AccessToken, result.IsNewUser));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        var result = await _refreshTokenHandler.Handle(
            new RefreshTokenCommand(refreshToken ?? string.Empty),
            cancellationToken);

        return Ok(new RefreshResponse(result.AccessToken));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        await _logoutHandler.Handle(
            new LogoutCommand(refreshToken ?? string.Empty),
            cancellationToken);

        DeleteRefreshTokenCookie();
        return NoContent();
    }

    [Authorize]
    [HttpPost("link-google")]
    public async Task<IActionResult> LinkGoogle([FromBody] LinkGoogleRequest request, CancellationToken cancellationToken)
    {
        var result = await _linkGoogleHandler.Handle(
            new LinkGoogleCommand(_currentUser.UsuarioId, request.IdToken),
            cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(new LinkGoogleResponse(result.UsuarioId, result.HogarId, result.AccessToken));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _forgotPasswordHandler.Handle(new ForgotPasswordCommand(request.Email), cancellationToken);
        return Ok(new { message = "If an account exists for that email, you will receive instructions." });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _resetPasswordHandler.Handle(
            new ResetPasswordCommand(request.Token, request.NewPassword, request.NewPasswordConfirmation),
            cancellationToken);

        return Ok(new { message = "Password reset successful." });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await _changePasswordHandler.Handle(
            new ChangePasswordCommand(_currentUser.UsuarioId, request.CurrentPassword, request.NewPassword, request.NewPasswordConfirmation),
            cancellationToken);

        return Ok(new { message = "Password updated." });
    }

    [Authorize]
    [HttpPost("add-password")]
    public async Task<IActionResult> AddPassword([FromBody] AddPasswordRequest request, CancellationToken cancellationToken)
    {
        await _addPasswordHandler.Handle(
            new AddPasswordCommand(_currentUser.UsuarioId, request.NewPassword, request.NewPasswordConfirmation),
            cancellationToken);

        return Ok(new { message = "Password added." });
    }

    private void SetRefreshTokenCookie(string? refreshToken)
    {
        if (refreshToken is null) return;

        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = _jwtOptions.Value.SecureCookies,
            Path = "/auth",
            MaxAge = TimeSpan.FromDays(_jwtOptions.Value.RefreshTokenExpiryDays)
        });
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Append("refreshToken", "", new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Path = "/auth",
            Expires = DateTimeOffset.UnixEpoch
        });
    }
}
