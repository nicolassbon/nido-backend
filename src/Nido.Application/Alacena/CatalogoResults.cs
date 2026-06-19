namespace Nido.Application.Alacena;

public sealed record CategoriaResult(Guid Id, string Nombre, int? TtlDias);
public sealed record UnidadMedidaResult(Guid Id, string Codigo, string Nombre);
public sealed record UbicacionResult(Guid Id, string Nombre, string? Icono, string? Color);
