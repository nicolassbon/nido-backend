namespace Nido.Application.Common.Security;

public interface ICurrentUserContext
{
    Guid UsuarioId { get; }
    Guid HogarId { get; }
}
