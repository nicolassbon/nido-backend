using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nido.Application.Productos;

namespace Nido.Infrastructure.Productos;

public sealed class PriceComparatorService : IPriceComparatorService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PriceComparatorService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ComparePricesResult> CompareAsync(string query, CancellationToken ct)
    {
        try
        {
            // El comparador expone su ruta principal /?q=termino
            var endpoint = $"?q={Uri.EscapeDataString(query)}";
            var result = await _httpClient.GetFromJsonAsync<ComparePricesResult>(endpoint, SerializerOptions, ct);
            
            return result ?? new ComparePricesResult(new(), new(), DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            // Propagamos el error indicando que falló el servicio de comparación
            throw new Exception("Error al comunicarse con el servicio del comparador de precios.", ex);
        }
    }
}
