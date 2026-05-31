using Nido.Domain.Exceptions;

namespace Nido.Application.Alacena.Exceptions;

public sealed class InvalidStockItemDateException : NidoException
{
    public InvalidStockItemDateException() : base("INVALID_STOCK_ITEM_DATE", "La fecha de vencimiento debe tener formato yyyy-MM-dd.") { }
}

public sealed class MissingStockItemFieldException : NidoException
{
    public MissingStockItemFieldException(string campo) : base("MISSING_STOCK_ITEM_FIELD", CampoAMensaje(campo)) { }

    private static string CampoAMensaje(string campo) => campo switch
    {
        "hogar" => "El hogar es requerido.",
        "cantidad" => "La cantidad es requerida.",
        _ => $"El campo {campo} es requerido."
    };
}
