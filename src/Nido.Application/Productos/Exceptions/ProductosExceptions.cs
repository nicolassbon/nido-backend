using Nido.Domain.Exceptions;

namespace Nido.Application.Productos.Exceptions;

public sealed class MissingProductFieldException : NidoException
{
    public MissingProductFieldException(string campo) : base("MISSING_PRODUCT_FIELD", CampoAMensaje(campo)) { }

    private static string CampoAMensaje(string campo) => campo switch
    {
        "nombre" => "El nombre es requerido.",
        "codigoBarras" => "El código de barras es requerido.",
        "cantidad" => "La cantidad debe ser mayor a cero.",
        "hogar" => "El hogar es requerido.",
        _ => $"El campo {campo} es requerido."
    };
}
