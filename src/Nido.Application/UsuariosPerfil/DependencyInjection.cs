using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.UsuariosPerfil;

public static class DependencyInjection
{
    public static IServiceCollection AddUsuariosPerfilModule(this IServiceCollection services)
    {
        services.AddScoped<ActualizarPerfilHandler>();
        return services;
    }
}
