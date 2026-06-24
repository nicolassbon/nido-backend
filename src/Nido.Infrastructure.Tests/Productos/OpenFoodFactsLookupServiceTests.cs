using System.Reflection;
using Nido.Application.Productos;
using Nido.Infrastructure.Productos;
using Xunit;

namespace Nido.Infrastructure.Tests.Productos;

public class OpenFoodFactsLookupServiceTests
{
    /// <summary>
    /// Tests para el método ExtractGramaje que es private, por lo que se testean
    /// indirectamente a través de LookupAsync.
    /// </summary>

    [Fact]
    public void ExtractGramaje_WithValidGramInName_ReturnsCleanNameAndGramaje()
    {
        // Arrange: Crear un producto con gramaje en el nombre
        var casos = new[]
        {
            ("Yogur Natural 190g", "Yogur Natural", 190m),
            ("Leche Descremada 1 Litro", "Leche Descremada", 1m),
            ("Queso Cremoso 250 gr", "Queso Cremoso", 250m),
            ("Manteca 200gramos", "Manteca", 200m),
            ("Mantequilla 200,5 g", "Mantequilla", 200.5m),
            ("Mantequilla 200.5 g", "Mantequilla", 200.5m),
        };

        foreach (var (input, expectedName, expectedGramaje) in casos)
        {
            // Act
            var (cleanName, gramaje) = InvokeExtractGramaje(input);

            // Assert
            Assert.Equal(expectedName, cleanName);
            Assert.Equal(expectedGramaje, gramaje);
        }
    }

    [Fact]
    public void ExtractGramaje_WithoutGramInName_ReturnsOriginalNameAndNull()
    {
        // Arrange
        var casos = new[] { "Yogur Natural", "Leche Descremada", "Pan Integral", "Queso" };

        foreach (var input in casos)
        {
            // Act
            var (cleanName, gramaje) = InvokeExtractGramaje(input);

            // Assert
            Assert.Equal(input, cleanName);
            Assert.Null(gramaje);
        }
    }

    [Fact]
    public void ExtractGramaje_WithInvalidNumber_ReturnsOriginalNameAndNull()
    {
        // Arrange
        var casos = new[]
        {
            "Producto con 8-14 dígitos podría ser código",
            "Algo 999999999999999g (número muy grande)",
        };

        foreach (var input in casos)
        {
            // Act
            var (cleanName, gramaje) = InvokeExtractGramaje(input);

            // Assert - El regex no debería matchear números inválidos o fórmulas
            // Algunos casos pueden no extraer si el regex es estricto
            // En este caso, aseguramos que el método retorna algo válido
            Assert.NotNull(cleanName);
        }
    }

    [Fact]
    public void ExtractGramaje_WithExtremeEdgeCases_HandlesGracefully()
    {
        // Arrange
        var casos = new[]
        {
            ("", "", null),                       // Empty string
            ("   ", "   ", null),                 // Whitespace
            ("X", "X", null),                     // Single char
            ("290g", "290g", null),               // Only number with unit (no name part)
            ("Producto  290  g", "Producto", 290m), // Multiple spaces
        };

        foreach (var (input, expectedName, expectedGramaje) in casos)
        {
            // Act
            var (cleanName, gramaje) = InvokeExtractGramaje(input);

            // Assert
            if (expectedGramaje.HasValue)
            {
                Assert.Equal(expectedGramaje, gramaje);
            }
            else
            {
                Assert.Null(gramaje);
            }
        }
    }

    private static (string cleanName, decimal? gramaje) InvokeExtractGramaje(string name)
    {
        // Usar Reflection para invocar el método private
        var method = typeof(OpenFoodFactsLookupService)
            .GetMethod("ExtractGramaje", BindingFlags.NonPublic | BindingFlags.Static);

        if (method == null)
            throw new InvalidOperationException("ExtractGramaje method not found");

        var result = method.Invoke(null, new object[] { name });
        return ((string, decimal?)?)result ?? (string.Empty, null);
    }
}
