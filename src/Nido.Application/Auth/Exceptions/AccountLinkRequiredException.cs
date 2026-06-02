using Nido.Domain.Exceptions;

namespace Nido.Application.Auth.Exceptions;

public sealed class AccountLinkRequiredException : NidoException
{
    public AccountLinkRequiredException(string code, string message) : base(code, message) { }
}
