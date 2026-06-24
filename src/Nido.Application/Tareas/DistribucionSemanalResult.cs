namespace Nido.Application.Tareas;

public sealed record MiembroDistribucionResult(Guid UsuarioId, string Nombre, int Completadas);
public sealed record DistribucionDiaResult(string Dia, DateTime Fecha, List<MiembroDistribucionResult> Miembros);
