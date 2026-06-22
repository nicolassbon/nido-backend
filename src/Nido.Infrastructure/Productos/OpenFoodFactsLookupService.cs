using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nido.Application.Alacena;
using Nido.Application.Productos;

namespace Nido.Infrastructure.Productos;

/// <summary>
/// Resuelve códigos de barras consultando tres fuentes en cascada:
///   1. Open Food Facts (mundial)
///   2. Open Food Facts Argentina (mejor cobertura local)
///   3. UPC Item DB (cobertura EAN-13 / UPC-A amplia)
///
/// La cascada continúa cuando una fuente devuelve un resultado sin nombre. Si
/// una fuente posterior devuelve null, se preserva el resultado anterior (que
/// puede tener categorías/imagen útiles para estimar TTL).
/// </summary>
public sealed class OpenFoodFactsLookupService : IExternalProductLookupService
{
    private readonly HttpClient _http;
    private readonly ILogger<OpenFoodFactsLookupService> _logger;
    private readonly ProductCategoryMapper _categoryMapper;
    private readonly ExternalLookupOptions _options;

    public OpenFoodFactsLookupService(
        HttpClient http,
        ILogger<OpenFoodFactsLookupService> logger,
        ProductCategoryMapper categoryMapper,
        IOptions<ExternalLookupOptions> options)
    {
        _http           = http;
        _logger         = logger;
        _categoryMapper = categoryMapper;
        _options        = options.Value;
    }

    public async Task<LookupExternalProductoResult> LookupAsync(string barcode, CancellationToken ct)
    {
        var encoded = Uri.EscapeDataString(barcode);

        var p = await FetchFromOffAsync(_options.OffWorldBase, encoded, ct);
        if (string.IsNullOrEmpty(p?.Name))
        {
            var p2 = await FetchFromOffAsync(_options.OffArBase, encoded, ct);
            p = p2 ?? p;
        }

        if (string.IsNullOrEmpty(p?.Name))
        {
            var p3 = await FetchFromUpcItemDbAsync(encoded, ct);
            p = p3 ?? p;
        }

        return p ?? Empty();
    }

    // ── OFF ──────────────────────────────────────────────────────────────────

    private async Task<LookupExternalProductoResult?> FetchFromOffAsync(string baseUrl, string encoded, CancellationToken ct)
    {
        var url = $"{baseUrl}/api/v0/product/{encoded}.json";
        try
        {
            var res = await _http.GetFromJsonAsync<OffApiResponse>(url, ct);
            if (res is null || res.Status != 1 || res.Product is null) return null;

            var p           = res.Product;
            var categories  = p.CategoriesTags ?? Array.Empty<string>();
            var n           = p.Nutriments;
            var sanitized   = SanitizeName(p.ProductNameEs ?? p.ProductName ?? p.ProductNameEn ?? string.Empty);
            var deBranded   = StripTrailingBrand(sanitized, p.Brands ?? string.Empty);
            var (cleanName, gramaje) = ExtractGramaje(deBranded);
            var nutrition = ToNutritionInfo(n);

            return new LookupExternalProductoResult(
                Name:              cleanName,
                Image:             p.ImageFrontUrl ?? p.ImageUrl ?? string.Empty,
                Brands:            p.Brands ?? string.Empty,
                CategoriesTags:    categories,
                CategoriaSugerida: _categoryMapper.Map(categories),
                FoundInDb:         true,
                Calorias:          nutrition?.Calorias,
                Proteinas:         nutrition?.Proteinas,
                Carbohidratos:     nutrition?.Carbohidratos,
                Grasas:            nutrition?.Grasas,
                GramajeExtraido:   gramaje,
                InformacionNutricional: nutrition
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OFF lookup falló para {BaseUrl} {Url}", baseUrl, url);
            return null;
        }
    }

    // ── UPC Item DB ──────────────────────────────────────────────────────────

    private async Task<LookupExternalProductoResult?> FetchFromUpcItemDbAsync(string encoded, CancellationToken ct)
    {
        var url = $"{_options.UpcItemDb}/prod/trial/lookup?upc={encoded}";
        try
        {
            var res = await _http.GetFromJsonAsync<UpcItemDbResponse>(url, ct);
            if (res is null || res.Code != "OK" || res.Items is null || res.Items.Count == 0) return null;

            var item        = res.Items[0];
            var categories  = ParseCategoryString(item.Category ?? string.Empty);
            var sanitized   = SanitizeName(item.Title ?? string.Empty);
            var deBranded   = StripTrailingBrand(sanitized, item.Brand ?? string.Empty);
            var (cleanName, gramaje) = ExtractGramaje(deBranded);

            return new LookupExternalProductoResult(
                Name:              cleanName,
                Image:             item.Images?.FirstOrDefault() ?? string.Empty,
                Brands:            item.Brand ?? string.Empty,
                CategoriesTags:    categories,
                CategoriaSugerida: _categoryMapper.Map(categories),
                FoundInDb:         true,
                GramajeExtraido:   gramaje
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UPC Item DB lookup falló para {Url}", url);
            return null;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve cadena vacía cuando el nombre es solo dígitos (8–14) — ocurre
    /// en productos mal catalogados donde se usa el código como placeholder.
    /// </summary>
    private static string SanitizeName(string raw)
    {
        var trimmed = raw.Trim();
        return Regex.IsMatch(trimmed, @"^\d{8,14}$") ? string.Empty : trimmed;
    }

    /// <summary>
    /// Saca la marca del final del nombre cuando aparece como token final
    /// (ej: "Queso blanco light x 290 Tregar" + marca "Tregar" → "Queso blanco light x 290").
    /// Las marcas de OFF pueden venir separadas por coma.
    /// </summary>
    private static string StripTrailingBrand(string name, string brands)
    {
        if (string.IsNullOrWhiteSpace(brands)) return name.Trim();

        var result = name.Trim();
        foreach (var brand in brands.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (brand.Length < 2) continue;
            result = Regex.Replace(result, $@"\s+{Regex.Escape(brand)}\s*$", string.Empty, RegexOptions.IgnoreCase).Trim();
        }

        return result;
    }

    /// <summary>
    /// Extrae el gramaje del final del nombre, tolerando el prefijo "x" típico de
    /// Argentina y unidades de peso/volumen:
    ///   "Producto 290g"      → ("Producto", 290)
    ///   "Producto x 290"     → ("Producto", 290)
    ///   "Producto x290 g"    → ("Producto", 290)
    /// Si no encuentra gramaje, devuelve (name, null).
    /// </summary>
    private static (string cleanName, decimal? gramaje) ExtractGramaje(string name)
    {
        var trimmed = name.Trim();

        // Formato argentino "Producto x 290 [marca]": cortamos todo desde la "x"
        // y nos quedamos con el número, aunque haya texto (marca) después.
        var xMatch = Regex.Match(trimmed, @"^(.+?)\s+x\s*(\d+(?:[.,]\d+)?)\b", RegexOptions.IgnoreCase);
        if (xMatch.Success && TryParseGramaje(xMatch.Groups[2].Value, out var xg))
        {
            return (xMatch.Groups[1].Value.Trim(), xg);
        }

        // Gramaje al final con unidad opcional: "Producto 290g".
        var endMatch = Regex.Match(
            trimmed,
            @"^(.+?)\s+(\d+(?:[.,]\d+)?)\s*(?:gr|grs|g|gramos|kg|ml|cc|cm3|lt|l|litros?)?\s*$",
            RegexOptions.IgnoreCase);
        if (endMatch.Success && TryParseGramaje(endMatch.Groups[2].Value, out var eg))
        {
            return (endMatch.Groups[1].Value.Trim(), eg);
        }

        return (trimmed, null);
    }

    private static bool TryParseGramaje(string raw, out decimal value) =>
        decimal.TryParse(raw.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture, out value);

    /// <summary>
    /// "Food &amp; Grocery > Dairy > Spreads" → ["en:food-grocery", "en:dairy", "en:spreads"]
    /// </summary>
    private static string[] ParseCategoryString(string category)
    {
        return category
            .Split('>', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToLowerInvariant())
            .Select(s => Regex.Replace(s, "[^a-z0-9]+", "-"))
            .Select(s => s.Trim('-'))
            .Where(s => s.Length > 0)
            .Select(s => $"en:{s}")
            .ToArray();
    }

    private static LookupExternalProductoResult Empty() =>
        new(string.Empty, string.Empty, string.Empty, Array.Empty<string>(), ProductCategoryMapper.General, false, null, null, null, null, null);

    private static NutritionInfoResult? ToNutritionInfo(OffNutriments? nutriments)
    {
        if (nutriments is null)
        {
            return null;
        }

        var items = new List<NutritionInfoItemResult>();
        AddNutritionItem(items, "Valor energetico", nutriments.EnergyKcal100g, "kcal");
        AddNutritionItem(items, "Proteinas", nutriments.Proteins100g, "g");
        AddNutritionItem(items, "Carbohidratos", nutriments.Carbohydrates100g, "g");
        AddNutritionItem(items, "Azucares", nutriments.Sugars100g, "g");
        AddNutritionItem(items, "Grasas", nutriments.Fat100g, "g");
        AddNutritionItem(items, "Grasas saturadas", nutriments.SaturatedFat100g, "g");
        AddNutritionItem(items, "Fibra", nutriments.Fiber100g, "g");
        AddNutritionItem(items, "Sodio", nutriments.Sodium100g * 1000, "mg");
        AddNutritionItem(items, "Sal", nutriments.Salt100g, "g");

        if (items.Count == 0)
        {
            return null;
        }

        return new NutritionInfoResult(
            nutriments.EnergyKcal100g,
            nutriments.Proteins100g,
            nutriments.Carbohydrates100g,
            nutriments.Fat100g,
            null,
            "100 g",
            items);
    }

    private static void AddNutritionItem(
        List<NutritionInfoItemResult> items,
        string name,
        decimal? value,
        string unit)
    {
        if (!value.HasValue)
        {
            return;
        }

        items.Add(new NutritionInfoItemResult(name, value, unit, null, items.Count + 1));
    }

    // ── DTOs de las APIs externas ────────────────────────────────────────────

    private sealed class OffApiResponse
    {
        [JsonPropertyName("status")]   public int           Status  { get; set; }
        [JsonPropertyName("product")]  public OffProduct?   Product { get; set; }
    }

    private sealed class OffProduct
    {
        [JsonPropertyName("product_name_es")]  public string?   ProductNameEs  { get; set; }
        [JsonPropertyName("product_name")]     public string?   ProductName    { get; set; }
        [JsonPropertyName("product_name_en")]  public string?   ProductNameEn  { get; set; }
        [JsonPropertyName("image_front_url")]  public string?   ImageFrontUrl  { get; set; }
        [JsonPropertyName("image_url")]        public string?   ImageUrl       { get; set; }
        [JsonPropertyName("brands")]           public string?   Brands         { get; set; }
        [JsonPropertyName("categories_tags")]  public string[]? CategoriesTags { get; set; }
        [JsonPropertyName("nutriments")]       public OffNutriments? Nutriments { get; set; }
    }

    // Los valores "*_100g" vienen como número en la API de OFF. Permitimos
    // leerlos desde string por robustez ante variaciones del dato.
    private sealed class OffNutriments
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        [JsonPropertyName("energy-kcal_100g")]   public decimal? EnergyKcal100g    { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        [JsonPropertyName("proteins_100g")]      public decimal? Proteins100g      { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        [JsonPropertyName("carbohydrates_100g")] public decimal? Carbohydrates100g { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        [JsonPropertyName("fat_100g")]           public decimal? Fat100g           { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        [JsonPropertyName("sugars_100g")]        public decimal? Sugars100g        { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        [JsonPropertyName("saturated-fat_100g")] public decimal? SaturatedFat100g  { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        [JsonPropertyName("fiber_100g")]         public decimal? Fiber100g         { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        [JsonPropertyName("sodium_100g")]        public decimal? Sodium100g        { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        [JsonPropertyName("salt_100g")]          public decimal? Salt100g          { get; set; }
    }

    private sealed class UpcItemDbResponse
    {
        [JsonPropertyName("code")]   public string?           Code  { get; set; }
        [JsonPropertyName("total")]  public int               Total { get; set; }
        [JsonPropertyName("items")]  public List<UpcItem>?    Items { get; set; }
    }

    private sealed class UpcItem
    {
        [JsonPropertyName("ean")]       public string?       Ean      { get; set; }
        [JsonPropertyName("title")]     public string?       Title    { get; set; }
        [JsonPropertyName("brand")]     public string?       Brand    { get; set; }
        [JsonPropertyName("images")]    public List<string>? Images   { get; set; }
        [JsonPropertyName("category")]  public string?       Category { get; set; }
    }
}
