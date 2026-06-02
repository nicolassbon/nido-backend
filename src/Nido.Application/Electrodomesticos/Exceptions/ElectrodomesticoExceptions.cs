using Nido.Domain.Exceptions;

namespace Nido.Application.Electrodomesticos.Exceptions;

public sealed class MissingApplianceFieldException : NidoException
{
    public MissingApplianceFieldException(string campo) : base("MISSING_APPLIANCE_FIELD", CampoAMensaje(campo)) { }

    private static string CampoAMensaje(string campo) => campo switch
    {
        "nombre" => "El nombre es requerido.",
        "hogar" => "El hogar es requerido.",
        _ => $"El campo {campo} es requerido."
    };
}

public sealed class HouseholdNotFoundException : NidoException
{
    public HouseholdNotFoundException() : base("HOUSEHOLD_NOT_FOUND", "El hogar indicado no existe.") { }
}
