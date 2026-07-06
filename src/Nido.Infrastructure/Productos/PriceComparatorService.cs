using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nido.Application.Productos;
using Nido.Application.Productos.Exceptions;

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
            var endpoint = $"?q={Uri.EscapeDataString(query)}";
            var result = await _httpClient.GetFromJsonAsync<ComparePricesResult>(endpoint, SerializerOptions, ct);

            return result ?? new ComparePricesResult(new(), new(), DateTime.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException)
        {
            throw new ComparatorUnavailableException(ex);
        }
    }
}
