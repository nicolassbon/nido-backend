using Nido.Application.Payments;

namespace Nido.Application.Tickets;

public sealed class ScanTicketHandler
{
    private readonly IReceiptParser _receiptParser;
    private readonly IEntitlementService _entitlementService;

    public ScanTicketHandler(
        IReceiptParser receiptParser,
        IEntitlementService entitlementService)
    {
        _receiptParser = receiptParser;
        _entitlementService = entitlementService;
    }

    public async Task<ScanTicketResult> Handle(ScanTicketCommand command, CancellationToken cancellationToken)
    {
        await _entitlementService.EnsurePremiumAsync(command.HogarId, cancellationToken);

        return await _receiptParser.ParseAsync(command.Image, cancellationToken);
    }
}
