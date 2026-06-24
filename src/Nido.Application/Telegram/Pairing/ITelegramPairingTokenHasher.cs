namespace Nido.Application.Telegram.Pairing;

public interface ITelegramPairingTokenHasher
{
    string Hash(string token);
}
