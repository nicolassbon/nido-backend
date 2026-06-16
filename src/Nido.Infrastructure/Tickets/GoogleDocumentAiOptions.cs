namespace Nido.Infrastructure.Tickets;

public sealed class GoogleDocumentAiOptions
{
    public const string SectionName = "GoogleDocumentAi";

    public string ProjectId { get; init; } = string.Empty;
    public string Location { get; init; } = "us";
    public string ProcessorId { get; init; } = string.Empty;
}