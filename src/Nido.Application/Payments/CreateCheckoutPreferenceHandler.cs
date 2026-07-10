namespace Nido.Application.Payments;

public sealed class CreateCheckoutPreferenceHandler
{
    private readonly IMercadoPagoGateway _gateway;

    public CreateCheckoutPreferenceHandler(IMercadoPagoGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<CreateCheckoutPreferenceResult> Handle(CreateCheckoutPreferenceCommand command, CancellationToken ct)
    {
        var preference = await _gateway.CreateCheckoutPreferenceAsync(
            new MercadoPagoCheckoutPreferenceRequest(command.HogarId),
            ct);

        return new CreateCheckoutPreferenceResult(preference.PreferenceId, preference.InitPoint.ToString());
    }
}

public sealed record CreateCheckoutPreferenceCommand(Guid HogarId);

public sealed record CreateCheckoutPreferenceResult(string PreferenceId, string InitPoint);
