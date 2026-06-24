namespace Nido.Api.Contracts.Tareas;

public sealed record MiembroDistribucionResponse(Guid UsuarioId, string Nombre, int Completadas);
public sealed record DistribucionDiaResponse(string Dia, DateTime Fecha, List<MiembroDistribucionResponse> Miembros);
public sealed record DistribucionSemanalResponse(List<DistribucionDiaResponse> Dias);
