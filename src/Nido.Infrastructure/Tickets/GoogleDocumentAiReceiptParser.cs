using Google.Api.Gax.Grpc;
using Google.Cloud.DocumentAI.V1;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Nido.Application.Common.Images;
using Nido.Application.Tickets;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Nido.Infrastructure.Tickets;

public sealed class GoogleDocumentAiReceiptParser : IReceiptParser
{
    private readonly GoogleDocumentAiOptions _options;

    public GoogleDocumentAiReceiptParser(IOptions<GoogleDocumentAiOptions> options)
    {
        _options = options.Value;
    }

    public async Task<ScanTicketResult> ParseAsync(ImageUpload image, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ProjectId) ||
            string.IsNullOrWhiteSpace(_options.Location) ||
            string.IsNullOrWhiteSpace(_options.ProcessorId))
        {
            throw new InvalidOperationException(
                "Missing Google Document AI configuration.");
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
                _options.ProcessorId).ToString(),
            RawDocument = new RawDocument
            {
                Content = ByteString.CopyFrom(image.Content),
                MimeType = image.ContentType
            }
        };

        var response = await client.ProcessDocumentAsync(
            request,
            CallSettings.FromCancellationToken(cancellationToken));

        var document = response.Document;

        var items = document.Entities
            .Where(entity => entity.Type == "line_item")
            .Select(ToTicketItem)
            .Where(item => item is not null)
            .Cast<ScanTicketItemResult>()
            .ToArray();

        return new ScanTicketResult(
            MerchantName: GetEntityText(document, "supplier_name"),
            Items: items);
    }

    private static ScanTicketItemResult? ToTicketItem(Document.Types.Entity entity)
    {
        var rawDescription = GetProperty(entity, "description") ?? entity.MentionText;
        var description = CleanText(rawDescription);
        if (string.IsNullOrWhiteSpace(rawDescription) ||
            string.IsNullOrWhiteSpace(description) ||
            IsBarcode(description) ||
            IsQuantityOnly(description))
        {
            return null;
        }

        var productName = ExtractProductName(rawDescription);
        if (string.IsNullOrWhiteSpace(productName) || IsBarcode(productName) || IsQuantityOnly(productName))
        {
            return null;
        }

        var unitPrice =
            ExtractUnitPrice(description) ??
            ParseDecimal(GetProperty(entity, "unit_price")) ??
            ParseDecimal(GetProperty(entity, "amount"));
        var quantity =
            ExtractQuantity(description) ??
            ParseDecimal(GetProperty(entity, "quantity")) ??
            1m;

        return new ScanTicketItemResult(productName, quantity, unitPrice);
    }

    private static string? GetEntityText(Document document, string type)
    {
        return document.Entities
            .Where(entity => entity.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(entity => entity.Confidence)
            .Select(entity => CleanText(entity.MentionText))
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
    }

    private static string? GetProperty(Document.Types.Entity entity, string name)
    {
        return entity.Properties
            .FirstOrDefault(property =>
                property.Type.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                property.Type.EndsWith($"/{name}", StringComparison.OrdinalIgnoreCase))
            ?.MentionText;
    }

    private static string ExtractProductName(string description)
    {
        return CleanText(description
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault()) ?? string.Empty;
    }

    private static decimal? ExtractUnitPrice(string description)
    {
        var match = Regex.Match(description, @"\b\d+(?:[.,]\d+)?\s*x\s*(?<price>\d+(?:[.,]\d{2})?)", RegexOptions.IgnoreCase);
        return match.Success ? ParseDecimal(match.Groups["price"].Value) : null;
    }

    private static decimal? ExtractQuantity(string description)
    {
        var match = Regex.Match(description, @"\b(?<quantity>\d+(?:[.,]\d+)?)\s*x\s*\d", RegexOptions.IgnoreCase);
        return match.Success ? ParseDecimal(match.Groups["quantity"].Value) : null;
    }

    private static bool IsBarcode(string value) =>
        Regex.IsMatch(value.Trim(), @"^\d{8,14}$");

    private static bool IsQuantityOnly(string value) =>
        decimal.TryParse(value.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out _);

    private static string? CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value
            .Replace("$", string.Empty)
            .Replace(" ", string.Empty)
            .Trim();

        if (normalized.Contains(',') && normalized.Contains('.'))
        {
            normalized = normalized.LastIndexOf(',') > normalized.LastIndexOf('.')
                ? normalized.Replace(".", string.Empty).Replace(',', '.')
                : normalized.Replace(",", string.Empty);
        }
        else if (normalized.Contains(','))
        {
            normalized = normalized.Replace(',', '.');
        }

        return decimal.TryParse(
            normalized,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var result)
            ? result
            : null;
    }
}
