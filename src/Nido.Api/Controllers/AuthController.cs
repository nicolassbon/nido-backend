using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Auth;
using Nido.Application.Auth;

namespace Nido.Api.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterUserHandler _registerUserHandler;
    private readonly LoginHandler _loginHandler;
    private readonly GoogleLoginHandler _googleLoginHandler;
    private readonly RefreshTokenHandler _refreshTokenHandler;
    private readonly LogoutHandler _logoutHandler;
    private readonly LinkGoogleHandler _linkGoogleHandler;

    public AuthController(
        RegisterUserHandler registerUserHandler,
        LoginHandler loginHandler,
        GoogleLoginHandler googleLoginHandler,
        RefreshTokenHandler refreshTokenHandler,
        LogoutHandler logoutHandler,
        LinkGoogleHandler linkGoogleHandler)
    {
        _registerUserHandler = registerUserHandler;
        _loginHandler = loginHandler;
        _googleLoginHandler = googleLoginHandler;
        _refreshTokenHandler = refreshTokenHandler;
        _logoutHandler = logoutHandler;
        _linkGoogleHandler = linkGoogleHandler;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _registerUserHandler.Handle(
            new RegisterUserCommand(request.Nombre, request.Email, request.Password, request.Sexo, request.FotoUrl),
            cancellationToken);

        if (result.RefreshToken is not null)
        {
            SetRefreshTokenCookie(result.RefreshToken);
        }

        return StatusCode(StatusCodes.Status201Created, new RegisterResponse(result.UsuarioId, result.HogarId, result.AccessToken));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _loginHandler.Handle(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(new LoginResponse(result.UsuarioId, result.HogarId, result.AccessToken));
    }

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _googleLoginHandler.Handle(
            new GoogleLoginCommand(request.IdToken),
            cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(new GoogleLoginResponse(result.UsuarioId, result.HogarId, result.AccessToken, result.IsNewUser));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        var result = await _refreshTokenHandler.Handle(
            new RefreshTokenCommand(refreshToken ?? string.Empty),
            cancellationToken);

        return Ok(new RefreshResponse(result.AccessToken));
    }

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

    [HttpPost("link-google")]
    public async Task<IActionResult> LinkGoogle([FromBody] LinkGoogleRequest request, CancellationToken cancellationToken)
    {
        var result = await _linkGoogleHandler.Handle(
            new LinkGoogleCommand(request.IdToken, request.Password),
            cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(new LinkGoogleResponse(result.UsuarioId, result.HogarId, result.AccessToken));
    }

    private void SetRefreshTokenCookie(string? refreshToken)
    {
        if (refreshToken is null) return;

        var isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";

        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = isProduction,
            Path = "/auth",
            MaxAge = TimeSpan.FromDays(7)
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
