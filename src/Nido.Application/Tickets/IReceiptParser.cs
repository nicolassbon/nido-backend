using Nido.Application.Common.Images;

namespace Nido.Application.Tickets;

public interface IReceiptParser
{
    Task<ScanTicketResult> ParseAsync(ImageUpload image, CancellationToken cancellationToken);
}