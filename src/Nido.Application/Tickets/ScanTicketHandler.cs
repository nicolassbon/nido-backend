namespace Nido.Application.Tickets;

public sealed class ScanTicketHandler
{
    private readonly IReceiptParser _receiptParser;

    public ScanTicketHandler(IReceiptParser receiptParser)
    {
        _receiptParser = receiptParser;
    }

    public Task<ScanTicketResult> Handle(ScanTicketCommand command, CancellationToken cancellationToken)
    {
        return _receiptParser.ParseAsync(command.Image, cancellationToken);
    }
}