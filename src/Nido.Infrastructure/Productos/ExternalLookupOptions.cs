namespace Nido.Infrastructure.Productos;

/// <summary>
/// Configuración del servicio que consulta APIs externas (Open Food Facts,
/// UPC Item DB) para resolver códigos de barras. Se bindea desde
/// appsettings.json en la sección "ExternalLookup".
/// </summary>
public sealed class ExternalLookupOptions
{
    public const string SectionName = "ExternalLookup";

    public string OffWorldBase { get; set; } = "https://world.openfoodfacts.org";
    public string OffArBase    { get; set; } = "https://ar.openfoodfacts.org";
    public string UpcItemDb    { get; set; } = "https://api.upcitemdb.com";

    /// <summary>User-Agent que requiere Open Food Facts.</summary>
    public string UserAgent { get; set; } = "NidoApp/1.0";

    /// <summary>Timeout total por intento contra cada API externa.</summary>
    public int TimeoutSeconds { get; set; } = 8;

    /// <summary>Cantidad de horas que se cachea el resultado del lookup por barcode.</summary>
    public int CacheHours { get; set; } = 24;
}
