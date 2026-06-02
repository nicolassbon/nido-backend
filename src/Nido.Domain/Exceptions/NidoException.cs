namespace Nido.Domain.Exceptions;

public abstract class NidoException : Exception
{
    public string Code { get; }

    protected NidoException(string code, string message) : base(message)
    {
        Code = code;
    }

    protected NidoException(string code, string message, Exception inner) : base(message, inner)
    {
        Code = code;
    }
}
