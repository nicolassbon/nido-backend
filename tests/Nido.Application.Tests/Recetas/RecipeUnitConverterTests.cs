using Nido.Application.Recetas;

namespace Nido.Application.Tests.Recetas;

public sealed class RecipeUnitConverterTests
{
    [Fact]
    public void ToShoppingListQuantity_ConTazasDeLiquido_DevuelveMililitros()
    {
        var result = RecipeUnitConverter.ToShoppingListQuantity(2m, "taza", "Agua");

        Assert.Equal(new IngredientQuantity(480m, "ml"), result);
    }

    [Fact]
    public void ToShoppingListQuantity_ConTazaDeHarina_DevuelveGramos()
    {
        var result = RecipeUnitConverter.ToShoppingListQuantity(1m, "taza", "Harina");

        Assert.Equal(new IngredientQuantity(120m, "g"), result);
    }

    [Fact]
    public void ToShoppingListQuantity_ConMediaTazaEmbebida_DevuelveMililitros()
    {
        var result = RecipeUnitConverter.ToShoppingListQuantity(null, "1/2 taza", "Caldo");

        Assert.Equal(new IngredientQuantity(120m, "ml"), result);
    }

    [Fact]
    public void ToShoppingListQuantity_ConCucharadasDeSal_DevuelveGramos()
    {
        var result = RecipeUnitConverter.ToShoppingListQuantity(2m, "cda", "Sal");

        Assert.Equal(new IngredientQuantity(36m, "g"), result);
    }

    [Fact]
    public void ToShoppingListQuantity_ConCondimentosSecos_DevuelveGramos()
    {
        var chile = RecipeUnitConverter.ToShoppingListQuantity(1m, "cdta", "Chile en polvo");
        var pimenton = RecipeUnitConverter.ToShoppingListQuantity(1m, "cdta", "Pimentón dulce");
        var oregano = RecipeUnitConverter.ToShoppingListQuantity(2m, "cdta", "Orégano seco");

        Assert.Equal(new IngredientQuantity(2.5m, "g"), chile);
        Assert.Equal(new IngredientQuantity(2.5m, "g"), pimenton);
        Assert.Equal(new IngredientQuantity(5m, "g"), oregano);
    }

    [Fact]
    public void ToShoppingListQuantity_NormalizaMasaYVolumen()
    {
        var harina = RecipeUnitConverter.ToShoppingListQuantity(1500m, "g", "Harina");
        var leche = RecipeUnitConverter.ToShoppingListQuantity(1500m, "ml", "Leche");

        Assert.Equal(new IngredientQuantity(1.5m, "kg"), harina);
        Assert.Equal(new IngredientQuantity(1.5m, "lt"), leche);
    }

    [Fact]
    public void ToShoppingListQuantity_ConUnidadNoConvertible_ConservaOriginal()
    {
        var result = RecipeUnitConverter.ToShoppingListQuantity(1m, "atado", "Perejil");

        Assert.Equal(new IngredientQuantity(1m, "atado"), result);
    }

    [Fact]
    public void ConvertQuantity_MantieneConversionesDeCocinar()
    {
        var harina = RecipeUnitConverter.ConvertQuantity(1m, "taza", "g", "Harina");
        var agua = RecipeUnitConverter.ConvertQuantity(2m, "taza", "lt", "Agua");

        Assert.Equal(120m, harina);
        Assert.Equal(0.48m, agua);
    }
}
