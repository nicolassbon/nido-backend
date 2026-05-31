using Nido.Domain.Exceptions;

namespace Nido.Application.Auth;

public sealed class AccountLinkRequiredException : NidoException
{
    public AccountLinkRequiredException(string code, string message) : base(code, message) { }
}
