using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
            var (cleanName, gramaje) = ExtractGramaje(sanitized);

            return new LookupExternalProductoResult(
                Name:              cleanName,
                Image:             p.ImageFrontUrl ?? p.ImageUrl ?? string.Empty,
                Brands:            p.Brands ?? string.Empty,
                CategoriesTags:    categories,
                CategoriaSugerida: _categoryMapper.Map(categories),
                FoundInDb:         true,
                Calorias:          n?.EnergyKcal100g,
                Proteinas:         n?.Proteins100g,
                Carbohidratos:     n?.Carbohydrates100g,
                Grasas:            n?.Fat100g,
                GramajeExtraido:   gramaje
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
            var (cleanName, gramaje) = ExtractGramaje(sanitized);

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
    /// Extrae el gramaje del final del nombre (ej: "Producto 290g" → (name: "Producto", gramaje: 290m))
    /// Si no encuentra gramaje, devuelve (name, null)
    /// </summary>
    private static (string cleanName, decimal? gramaje) ExtractGramaje(string name)
    {
        var trimmed = name.Trim();
        var match = Regex.Match(trimmed, @"^(.+?)\s+(\d+(?:[.,]\d+)?)\s*(?:gr|g|gramos)?$", RegexOptions.IgnoreCase);

        if (!match.Success) return (trimmed, null);

        var cleanName = match.Groups[1].Value.Trim();
        var gramStr = match.Groups[2].Value.Replace(",", ".");

        if (decimal.TryParse(gramStr, System.Globalization.CultureInfo.InvariantCulture, out var gramaje))
        {
            return (cleanName, gramaje);
        }

        return (trimmed, null);
    }

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
