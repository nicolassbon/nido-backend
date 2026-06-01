using Microsoft.Extensions.DependencyInjection;
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

namespace Nido.Application.Auth;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services)
    {
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<GoogleLoginHandler>();
        services.AddScoped<RefreshTokenHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<LinkGoogleHandler>();
        services.AddScoped<ForgotPasswordHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<AddPasswordHandler>();
        return services;
    }
}
