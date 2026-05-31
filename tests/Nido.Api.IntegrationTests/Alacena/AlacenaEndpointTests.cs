using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Nido.Application.Auth;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests.Alacena;

public sealed class AlacenaEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public AlacenaEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CrudProductoStock_PreservesHttpContract()
    {
        Guid hogarId;
        Guid usuarioId;

        using (var scope = _factory.Services.CreateScope())
        {
            var authRepository = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var createdUser = await authRepository.CreateUserWithPasswordAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "User Test",
                $"{Guid.NewGuid():N}@test.com",
                "hash",
                "U",
                null,
                CancellationToken.None);

            usuarioId = createdUser.UsuarioId;
            hogarId = createdUser.HogarId;
        }

        var createResponse = await _client.PostAsJsonAsync("/api/alacena/productos", new
        {
            nombre = "Yerba",
            codigoBarras = "779999",
            imagen = "https://img.test/yerba.png",
            ubicacion = "Alacena",
            cantidad = 1,
            fechaVencimiento = "2026-12-01",
            estaAbierto = false,
            porcentajeConsumido = 0,
            hogarId,
            usuarioId
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<StockItemBody>();
        Assert.NotNull(created);

        var listResponse = await _client.GetAsync($"/api/alacena/productos?hogarId={hogarId}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<List<StockItemBody>>();
        Assert.NotNull(list);
        Assert.NotEmpty(list!);

        var patchResponse = await _client.PatchAsJsonAsync($"/api/alacena/productos/{created!.Id}", new
        {
            cantidad = 2,
            ubicacion = "Heladera",
            fechaVencimiento = "2026-12-02",
            estaAbierto = true,
            porcentajeConsumido = 25,
            usuarioId
        });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/api/alacena/productos/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    private sealed record StockItemBody(Guid Id, Guid ProductoId, string Nombre, string? Imagen, string? CodigoBarras, string Ubicacion, decimal Cantidad, string? FechaVencimiento, bool EstaAbierto, decimal PorcentajeConsumido);
}
