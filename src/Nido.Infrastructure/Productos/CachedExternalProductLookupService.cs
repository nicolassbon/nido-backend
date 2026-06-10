using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Nido.Application.Productos;

namespace Nido.Infrastructure.Productos;

/// <summary>
/// Decorador de <see cref="IExternalProductLookupService"/> que cachea el
/// resultado por barcode en memoria. TTL configurable (default 24h).
///
/// El cache vive en memoria del proceso (no Redis), suficiente para MVP. Si la
/// API crece a múltiples instancias se puede swappear por un IDistributedCache
/// sin tocar al consumidor.
/// </summary>
public sealed class CachedExternalProductLookupService : IExternalProductLookupService
{
    private const string CacheKeyPrefix = "external-lookup:";

    private readonly IExternalProductLookupService _inner;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _ttl;

    public CachedExternalProductLookupService(
        IExternalProductLookupService inner,
        IMemoryCache cache,
        IOptions<ExternalLookupOptions> options)
    {
        _inner = inner;
        _cache = cache;
        _ttl   = TimeSpan.FromHours(Math.Max(1, options.Value.CacheHours));
    }

    public async Task<LookupExternalProductoResult> LookupAsync(string barcode, CancellationToken ct)
    {
        var key = CacheKeyPrefix + barcode;

        if (_cache.TryGetValue<LookupExternalProductoResult>(key, out var cached) && cached is not null)
            return cached;

        var fresh = await _inner.LookupAsync(barcode, ct);

        // Solo cacheamos lookups exitosos: si no encontró nada, queremos que
        // un próximo intento (con la API ya estable) tenga otra chance.
        if (fresh.FoundInDb)
        {
            _cache.Set(key, fresh, _ttl);
        }

        return fresh;
    }
}
