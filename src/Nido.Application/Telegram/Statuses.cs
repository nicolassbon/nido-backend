namespace Nido.Application.Telegram;

public enum TelegramOutboxStatus
{
    Pending = 0,
    Ready = 1,
    Sent = 2,
    Failed = 3,
    Dead = 4
}

public enum TelegramPairingStatus
{
    Pending = 0,
    Consumed = 1,
    Expired = 2,
    Revoked = 3
}

public enum TelegramBatchStatus
{
    Pending = 0,
    Ready = 1,
    Sent = 2,
    Failed = 3,
    Dead = 4
}
