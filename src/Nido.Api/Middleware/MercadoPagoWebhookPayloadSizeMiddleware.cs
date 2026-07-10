using Microsoft.AspNetCore.Http;

namespace Nido.Api.Middleware;

public sealed class MercadoPagoWebhookPayloadSizeMiddleware
{
    public const int MaxPayloadBytes = 64 * 1024;
    private const string RoutePath = "/api/webhooks/mercadopago";

    private readonly RequestDelegate _next;

    public MercadoPagoWebhookPayloadSizeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsWebhookRequest(context.Request))
        {
            await _next(context);
            return;
        }

        if (context.Request.ContentLength is long length && length > MaxPayloadBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        var buffered = await BufferBoundedBodyAsync(context.Request.Body, context.RequestAborted);
        if (buffered.Oversized)
        {
            await buffered.Stream.DisposeAsync();
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        context.Request.Body = buffered.Stream;
        context.Request.ContentLength = buffered.Stream.Length;
        try
        {
            await _next(context);
        }
        finally
        {
            await buffered.Stream.DisposeAsync();
        }
    }

    private static bool IsWebhookRequest(HttpRequest request)
        => HttpMethods.IsPost(request.Method)
           && string.Equals(request.Path, RoutePath, StringComparison.OrdinalIgnoreCase);

    private static async Task<BoundedBuffer> BufferBoundedBodyAsync(Stream body, CancellationToken cancellationToken)
    {
        var memory = new MemoryStream(capacity: Math.Min(MaxPayloadBytes, 4096));
        var buffer = new byte[4096];
        var total = 0L;
        var limit = MaxPayloadBytes + 1L;

        while (total < limit)
        {
            var remaining = (int)Math.Min(buffer.Length, limit - total);
            var read = await body.ReadAsync(buffer.AsMemory(0, remaining), cancellationToken);
            if (read == 0)
            {
                break;
            }

            memory.Write(buffer, 0, read);
            total += read;
        }

        memory.Position = 0;
        return new BoundedBuffer(memory, total > MaxPayloadBytes);
    }

    private readonly record struct BoundedBuffer(MemoryStream Stream, bool Oversized);
}
