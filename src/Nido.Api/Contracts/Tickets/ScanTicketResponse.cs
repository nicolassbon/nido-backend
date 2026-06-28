namespace Nido.Api.Contracts.Tickets;

public sealed record ScanTicketResponse(
    string? MerchantName,
    IReadOnlyCollection<ScanTicketItemResponse> Items);

public sealed record ScanTicketItemResponse(
    string ProductName,
    decimal Quantity,
    decimal? UnitPrice);
