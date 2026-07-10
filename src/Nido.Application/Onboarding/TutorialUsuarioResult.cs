namespace Nido.Application.Onboarding;

public sealed record TutorialUsuarioResult(
    Guid Id,
    Guid UsuarioId,
    bool HomeCompletado,
    bool AlacenaCompletado,
    bool RecetasCompletado,
    bool ListaComprasCompletado,
    bool ElectrodomesticosCompletado,
    bool FinanzasCompletado,
    bool PlanificadorCompletado,
    bool TareasCompletado,
    bool NotificacionesCompletado,
    bool PerfilCompletado,
    bool ConfiguracionCompletado);
