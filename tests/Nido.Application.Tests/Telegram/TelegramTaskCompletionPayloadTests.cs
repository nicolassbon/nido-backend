using Nido.Application.Telegram.Conversation;
using Xunit;

namespace Nido.Application.Tests.Telegram;

public sealed class TelegramTaskCompletionPayloadTests
{
    [Fact]
    public void TryParse_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(TelegramTaskCompletionPayload.TryParse(null));
        Assert.Null(TelegramTaskCompletionPayload.TryParse(string.Empty));
        Assert.Null(TelegramTaskCompletionPayload.TryParse("   "));
    }

    [Fact]
    public void TryParse_WrongFlow_ReturnsNull()
    {
        const string json = """{"flow":"other.thing","choices":[]}""";

        Assert.Null(TelegramTaskCompletionPayload.TryParse(json));
    }

    [Fact]
    public void TryParse_InvalidJson_ReturnsNull()
    {
        const string json = """{not valid""";

        Assert.Null(TelegramTaskCompletionPayload.TryParse(json));
    }

    [Fact]
    public void TryParse_ValidJson_ReturnsPayloadWithChoices()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var json = $$"""
        {
          "flow": "tasks.complete",
          "choices": [
            { "index": 1, "taskId": "{{firstId}}" },
            { "index": 2, "taskId": "{{secondId}}" }
          ]
        }
        """;

        var payload = TelegramTaskCompletionPayload.TryParse(json);

        Assert.NotNull(payload);
        Assert.Equal(TelegramTaskCompletionPayload.TasksCompleteFlow, payload!.Flow);
        Assert.Equal(2, payload.Choices.Count);
        Assert.Equal(1, payload.Choices[0].Index);
        Assert.Equal(firstId, payload.Choices[0].TaskId);
        Assert.Equal(2, payload.Choices[1].Index);
        Assert.Equal(secondId, payload.Choices[1].TaskId);
    }

    [Fact]
    public void TryFindChoice_ExistingIndex_ReturnsChoice()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var payload = new TelegramTaskCompletionPayload(
            TelegramTaskCompletionPayload.TasksCompleteFlow,
            new[]
            {
                new TelegramTaskCompletionChoice(1, firstId),
                new TelegramTaskCompletionChoice(2, secondId)
            });

        var found = payload.TryFindChoice(2, out var choice);

        Assert.True(found);
        Assert.NotNull(choice);
        Assert.Equal(2, choice!.Index);
        Assert.Equal(secondId, choice.TaskId);
    }

    [Fact]
    public void TryFindChoice_OutOfRangeIndex_ReturnsFalse()
    {
        var payload = new TelegramTaskCompletionPayload(
            TelegramTaskCompletionPayload.TasksCompleteFlow,
            new[]
            {
                new TelegramTaskCompletionChoice(1, Guid.NewGuid())
            });

        var found = payload.TryFindChoice(9, out var choice);

        Assert.False(found);
        Assert.Null(choice);
    }

    [Fact]
    public void Serialize_RoundtripsThroughTryParse()
    {
        var payload = new TelegramTaskCompletionPayload(
            TelegramTaskCompletionPayload.TasksCompleteFlow,
            new[]
            {
                new TelegramTaskCompletionChoice(1, Guid.NewGuid()),
                new TelegramTaskCompletionChoice(2, Guid.NewGuid())
            });

        var json = payload.Serialize();
        var roundtrip = TelegramTaskCompletionPayload.TryParse(json);

        Assert.NotNull(roundtrip);
        Assert.Equal(payload.Flow, roundtrip!.Flow);
        Assert.Equal(payload.Choices.Count, roundtrip.Choices.Count);
        for (var i = 0; i < payload.Choices.Count; i++)
        {
            Assert.Equal(payload.Choices[i].Index, roundtrip.Choices[i].Index);
            Assert.Equal(payload.Choices[i].TaskId, roundtrip.Choices[i].TaskId);
        }
    }
}
