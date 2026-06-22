using Google.Api.Gax.Grpc;
using Google.Cloud.DocumentAI.V1;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Nido.Application.Alacena;
using Nido.Application.Common.Images;

namespace Nido.Infrastructure.Tickets;

public sealed class GoogleDocumentAiNutritionLabelParser : INutritionLabelParser
{
    private readonly GoogleDocumentAiOptions _options;

    public GoogleDocumentAiNutritionLabelParser(IOptions<GoogleDocumentAiOptions> options)
    {
        _options = options.Value;
    }

    public async Task<NutritionInfoResult> ParseAsync(ImageUpload image, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ProjectId) ||
            string.IsNullOrWhiteSpace(_options.Location) ||
            string.IsNullOrWhiteSpace(_options.NutritionProcessorId))
        {
            throw new InvalidOperationException("Missing Google Document AI nutrition configuration.");
        }

        var client = new DocumentProcessorServiceClientBuilder
        {
            Endpoint = $"{_options.Location}-documentai.googleapis.com"
        }.Build();

        var request = new ProcessRequest
        {
            Name = ProcessorName.FromProjectLocationProcessor(
                _options.ProjectId,
                _options.Location,
                _options.NutritionProcessorId).ToString(),
            RawDocument = new RawDocument
            {
                Content = ByteString.CopyFrom(image.Content),
                MimeType = image.ContentType
            }
        };

        var response = await client.ProcessDocumentAsync(
            request,
            CallSettings.FromCancellationToken(cancellationToken));

        return NutritionLabelTextParser.Parse(response.Document.Text);
    }
}

