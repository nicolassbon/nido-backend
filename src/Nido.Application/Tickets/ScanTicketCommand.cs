using Nido.Application.Common.Images;

namespace Nido.Application.Tickets;

public  record ScanTicketCommand(Guid HogarId, ImageUpload Image);

public  record ScanTicketResult(
    string? MerchantName,
    IReadOnlyCollection<ScanTicketItemResult> Items);

public  record ScanTicketItemResult(
    string ProductName,
    decimal Quantity,
    decimal? UnitPrice);
