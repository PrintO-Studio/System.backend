using PrintO.Intergrations.Interfaces;
using PrintO.Models;
using PrintO.Models.Products.Figurine;
using System.Text.Json;

namespace PrintO.Intergrations;

public class WildberriesIntegration : IIntegradable<FigurineReference, FigurineVariation>
{
    public bool HasFigurine(string SKU, out JsonElement root)
    {
        throw new NotImplementedException();
    }

    public bool UpdateAllFigurines(User executor, int storeId)
    {
        throw new NotImplementedException();
    }

    public bool UpdateFigurine(User executor, FigurineReference product)
    {
        throw new NotImplementedException();
    }
}