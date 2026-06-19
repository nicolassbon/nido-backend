using Nido.Domain.Exceptions;

namespace Nido.Application.Finanzas.Exceptions;

public sealed class InvalidGastoMontoException : NidoException
{
    public InvalidGastoMontoException() : base("INVALID_GASTO_MONTO", "El monto debe ser mayor a cero.") { }
}

public sealed class InvalidGastoFechaException : NidoException
{
    public InvalidGastoFechaException() : base("INVALID_GASTO_FECHA", "La fecha debe tener formato yyyy-MM-dd.") { }
}

public sealed class InvalidGastoCategoriaException : NidoException
{
    public InvalidGastoCategoriaException(string categoria)
        : base("INVALID_GASTO_CATEGORIA", $"La categoría '{categoria}' no es válida. Las categorías permitidas son: Comida, Transporte, Servicios, Otros.") { }
}

public sealed class InvalidFacturaTipoException : NidoException
{
    public InvalidFacturaTipoException(string tipo)
        : base("INVALID_FACTURA_TIPO", $"El tipo '{tipo}' no es válido. Los tipos permitidos son: servicio, suscripcion.") { }
}

public sealed class InvalidFacturaArchivoException : NidoException
{
    public InvalidFacturaArchivoException()
        : base("INVALID_FACTURA_ARCHIVO", "Solo se permiten archivos de tipo imagen (jpeg, png, webp) o PDF.") { }
}

public sealed class FacturaNotFoundException : NidoException
{
    public FacturaNotFoundException()
        : base("FACTURA_NOT_FOUND", "La factura no fue encontrada.") { }
}

public sealed class GastoNotFoundException : NidoException
{
    public GastoNotFoundException()
        : base("GASTO_NOT_FOUND", "El gasto no fue encontrado.") { }
}
