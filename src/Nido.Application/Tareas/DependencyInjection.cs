using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.Tareas;

public static class DependencyInjection
{
    public static IServiceCollection AddTareasModule(this IServiceCollection services)
    {
        services.AddScoped<GetTareasHandler>();
        services.AddScoped<GetMisTareasHandler>();
        services.AddScoped<CreateTareaHandler>();
        services.AddScoped<UpdateTareaHandler>();
        services.AddScoped<CompletarTareaHandler>();
        services.AddScoped<AsignarTareaHandler>();
        services.AddScoped<DeleteTareaHandler>();
        services.AddScoped<GetDistribucionSemanalHandler>();
        return services;
    }
}
