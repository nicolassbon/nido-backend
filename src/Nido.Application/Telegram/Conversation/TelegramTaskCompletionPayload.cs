using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nido.Application.Telegram.Conversation;

public sealed record TelegramTaskCompletionPayload(
    string Flow,
    IReadOnlyList<TelegramTaskCompletionChoice> Choices)
{
    public const string TasksCompleteFlow = "tasks.complete";
    public string Serialize()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }
    public static TelegramTaskCompletionPayload? TryParse(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<TelegramTaskCompletionPayload>(payloadJson, JsonOptions);
            if (payload is null)
            {
                return null;
            }

            if (!string.Equals(payload.Flow, TasksCompleteFlow, StringComparison.Ordinal))
            {
                return null;
            }

            return payload;
        }
        catch (JsonException)
        {
            return null;
        }
    }
    public bool TryFindChoice(int index, out TelegramTaskCompletionChoice? choice)
    {
        foreach (var candidate in Choices)
        {
            if (candidate.Index == index)
            {
                choice = candidate;
                return true;
            }
        }

        choice = null;
        return false;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };
}

public sealed record TelegramTaskCompletionChoice(int Index, Guid TaskId);
