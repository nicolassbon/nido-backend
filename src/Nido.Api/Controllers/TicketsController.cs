using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nido.Api.Contracts.Tickets;
using Nido.Api.ImageUploads;
using Nido.Application.Tickets;

namespace Nido.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets")]
public sealed class TicketsController : ControllerBase
{
    private const long MaxTicketImageBytes = 10 * 1024 * 1024;
    private readonly ScanTicketHandler _scanTicketHandler;

    public TicketsController(ScanTicketHandler scanTicketHandler)
    {
        _scanTicketHandler = scanTicketHandler;
    }

    [HttpPost("scan")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Scan(
        IFormFile? file,
        CancellationToken ct)
    {
        var image = await ImageUploadFormReader.ReadAsync(file, MaxTicketImageBytes, ct);

        var result = await _scanTicketHandler.Handle(
            new ScanTicketCommand(image),
            ct);

        return Ok(ToResponse(result));
    }

    private static ScanTicketResponse ToResponse(ScanTicketResult result)
    {
        return new ScanTicketResponse(
            result.MerchantName,
            result.Items.Select(item => new ScanTicketItemResponse(
                item.ProductName,
                item.Quantity,
                item.UnitPrice)).ToArray());
    }
}
