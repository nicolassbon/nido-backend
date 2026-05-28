namespace Nido.Application.Auth;

public interface IPasswordHasher
{
    string Hash(string password);
}
