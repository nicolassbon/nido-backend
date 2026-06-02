using System.Security.Claims;
using Nido.Application.Common.Security;

namespace Nido.Api.Security;

public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UsuarioId => ReadGuid(System.Security.Claims.ClaimTypes.NameIdentifier) ?? ReadGuid(Nido.Application.Common.Security.ClaimTypes.UsuarioId)
        ?? throw new UnauthorizedAccessException("usuarioId claim is required");

    public Guid HogarId => ReadGuid(Nido.Application.Common.Security.ClaimTypes.HogarId)
        ?? throw new UnauthorizedAccessException("hogarId claim is required");

    private Guid? ReadGuid(string claimType)
    {
        var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);
        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}
