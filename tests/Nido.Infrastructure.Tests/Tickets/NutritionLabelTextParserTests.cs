using Nido.Infrastructure.Tickets;

namespace Nido.Infrastructure.Tests.Tickets;

public sealed class NutritionLabelTextParserTests
{
    [Fact]
    public void Parse_ReadsSpanishNutritionLabelWithCommaDecimals()
    {
        const string text = """
            Informacion nutricional
            Por 100 ml Por racion
            Valor energetico 184 kJ/44 kcal 460 kJ/110 kcal 6%
            Grasas 1,9 g 4,8 g 7%
            de las cuales saturadas 0,2 g 0,5 g 3%
            Hidratos de carbono 5,7 g 14 g 5%
            de los cuales azucares 2 g 5 g 6%
            Fibra alimentaria 1,1 g 2,8 g
            Proteinas 0,7 g 1,8 g 4%
            Sal 0,76 g 1,9 g 32%
            **1 racion = 250 ml
            """;

        var result = NutritionLabelTextParser.Parse(text);

        Assert.Equal(110m, result.Calorias);
        Assert.Equal(1.8m, result.Proteinas);
        Assert.Equal(14m, result.Carbohidratos);
        Assert.Equal(4.8m, result.Grasas);
        Assert.Equal("Por porcion", result.Base);
        Assert.Contains(result.Items, item => item.Nombre == "Grasas saturadas" && item.Valor == 0.5m);
        Assert.Contains(result.Items, item => item.Nombre == "Azucares" && item.Valor == 5m);
        Assert.Contains(result.Items, item => item.Nombre == "Fibra alimentaria" && item.Valor == 2.8m);
        Assert.Contains(result.Items, item => item.Nombre == "Sal" && item.Valor == 1.9m && item.PorcentajeDiario == 32m);
    }

    [Fact]
    public void Parse_KeepsKnownNutrientsEvenWhenOnlyOneColumnExists()
    {
        const string text = """
            Carbohidratos 30 gr
            Proteinas 20 gr
            Grasas 10 gr
            Sodio 120 mg
            """;

        var result = NutritionLabelTextParser.Parse(text);

        Assert.Equal(30m, result.Carbohidratos);
        Assert.Equal(20m, result.Proteinas);
        Assert.Equal(10m, result.Grasas);
        Assert.Contains(result.Items, item => item.Nombre == "Sodio" && item.Valor == 120m && item.Unidad == "mg");
    }

    [Fact]
    public void Parse_ReadsLabelsWhenOcrSplitsTableColumns()
    {
        const string text = """
            INFORMACION NUTRICIONAL
            Porcion: 69 g (1 medallon)
            Porcion
            %VD
            Valor energetico
            Carbohidratos
            Proteinas
            Grasas totales
            Grasas saturadas
            Grasas trans
            Fibra alimentaria
            Sodio
            199 kcal = 825 kJ
            0 g
            12 g
            17 g
            8,4 g
            0,5 g
            0,2 g
            414 mg
            """;

        var result = NutritionLabelTextParser.Parse(text);

        Assert.Equal(199m, result.Calorias);
        Assert.Equal(0m, result.Carbohidratos);
        Assert.Equal(12m, result.Proteinas);
        Assert.Equal(17m, result.Grasas);
        Assert.Contains(result.Items, item => item.Nombre == "Grasas saturadas" && item.Valor == 8.4m);
        Assert.Contains(result.Items, item => item.Nombre == "Grasas trans" && item.Valor == 0.5m);
        Assert.Contains(result.Items, item => item.Nombre == "Fibra alimentaria" && item.Valor == 0.2m);
        Assert.Contains(result.Items, item => item.Nombre == "Sodio" && item.Valor == 414m && item.Unidad == "mg");
    }
}
