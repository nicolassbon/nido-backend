using Nido.Application.Common.Images;

namespace Nido.Application.Tickets;

public sealed record ScanTicketCommand(ImageUpload Image);

public sealed record ScanTicketResult(
    string? MerchantName,
    IReadOnlyCollection<ScanTicketItemResult> Items);

public sealed record ScanTicketItemResult(
    string ProductName,
    decimal Quantity,
    decimal? UnitPrice);
