namespace Nido.Application.Auth;

public sealed class AccountLinkRequiredException : Exception
{
    public string Code { get; }

    public AccountLinkRequiredException(string code, string message) : base(message)
    {
        Code = code;
    }
}
