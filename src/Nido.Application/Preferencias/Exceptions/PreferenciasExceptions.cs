using Nido.Domain.Exceptions;

namespace Nido.Application.Preferencias.Exceptions;

public sealed class MissingPreferenceFieldException : NidoException
{
    public MissingPreferenceFieldException(string campo) : base("MISSING_PREFERENCE_FIELD", CampoAMensaje(campo)) { }

    private static string CampoAMensaje(string campo) => campo switch
    {
        "usuario" => "El usuario es requerido.",
        _ => $"El campo {campo} es requerido."
    };
}

public sealed class InvalidThemeModeException : NidoException
{
    public InvalidThemeModeException() : base("INVALID_THEME_MODE", "El tema preferido debe ser light, dark o system.") { }
}

public sealed class InvalidPreferenceRangeException : NidoException
{
    public InvalidPreferenceRangeException() : base("INVALID_PREFERENCE_RANGE", "Los días de alerta deben estar entre 1 y 365.") { }
}
