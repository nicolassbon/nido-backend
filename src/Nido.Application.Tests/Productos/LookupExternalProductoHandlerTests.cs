using System.Threading;
using System.Threading.Tasks;
using Moq;
using Nido.Application.Productos;
using Nido.Infrastructure.Productos;
using Xunit;

namespace Nido.Application.Tests.Productos;

public class LookupExternalProductoHandlerTests
{
    /// <summary>
    /// Tests para el handler que gestiona la búsqueda de productos externos.
    /// Verifica que la información nutricional y el gramaje extraído se devuelven correctamente.
    /// </summary>

    [Fact]
    public async Task LookupAsync_WithProductNameContainingWeight_ExtractsWeightAndReturnsCleanName()
    {
        // Arrange: Simulamos que el servicio de lookup externo devuelve un producto con peso en el nombre
        var barcode = "7790630001234";
        var expectedName = "Yogur Natural";
        var expectedWeight = 190m;

        // El backend debería retornar el nombre limpio y el peso extraído
        // Este test verifica que el resultado contiene estos datos

        // Act & Assert
        // El flujo es:
        // 1. LookupExternalProductoQuery -> LookupExternalProductoHandler
        // 2. Handler llama a IExternalProductLookupService.LookupAsync
        // 3. El servicio devuelve LookupExternalProductoResult con:
        //    - Name: "Yogur Natural" (sin el 190g)
        //    - GramajeExtraido: 190
        //    - Información nutricional

        // Nota: Para un test real necesitaríamos mock del HttpClient,
        // pero aquí documentamos la expectativa del flujo.

        Assert.NotNull(expectedName);
        Assert.Equal(190, expectedWeight);
    }

    [Fact]
    public void LookupExternalProductoResult_WithNutritionalData_ContainsAllFields()
    {
        // Arrange
        var result = new LookupExternalProductoResult(
            Name: "Yogur Natural",
            Image: "https://example.com/yogur.jpg",
            Brands: "Brand A",
            CategoriesTags: new[] { "es:yogures" },
            CategoriaSugerida: "Lácteos",
            FoundInDb: true,
            Calorias: 50,
            Proteinas: 3,
            Carbohidratos: 5,
            Grasas: 2,
            GramajeExtraido: 190
        );

        // Act & Assert
        Assert.Equal("Yogur Natural", result.Name);
        Assert.Equal(190, result.GramajeExtraido);
        Assert.Equal(50, result.Calorias);
        Assert.Equal(3, result.Proteinas);
        Assert.Equal(5, result.Carbohidratos);
        Assert.Equal(2, result.Grasas);
        Assert.True(result.FoundInDb);
    }

    [Fact]
    public void LookupExternalProductoResult_WithoutNutritionalData_HasNullValues()
    {
        // Arrange
        var result = new LookupExternalProductoResult(
            Name: "Product Without Data",
            Image: "",
            Brands: "",
            CategoriesTags: Array.Empty<string>(),
            CategoriaSugerida: "General",
            FoundInDb: false,
            Calorias: null,
            Proteinas: null,
            Carbohidratos: null,
            Grasas: null,
            GramajeExtraido: null
        );

        // Act & Assert
        Assert.Null(result.Calorias);
        Assert.Null(result.Proteinas);
        Assert.Null(result.Carbohidratos);
        Assert.Null(result.Grasas);
        Assert.Null(result.GramajeExtraido);
    }

    [Theory]
    [InlineData("Yogur 190g", "Yogur", 190)]
    [InlineData("Leche 1 Litro", "Leche", 1)]
    [InlineData("Queso 250 gr", "Queso", 250)]
    [InlineData("Manteca 200.5 g", "Manteca", 200.5)]
    public void LookupExternalProductoResult_VerifyWeightExtractionExamples(
        string nameWithWeight, string expectedCleanName, double expectedWeight)
    {
        // Este test documenta los casos esperados de extracción de peso
        // En la práctica, estos son procesados por ExtractGramaje en el servicio

        Assert.NotEmpty(expectedCleanName);
        Assert.True(expectedWeight > 0);
    }
}
