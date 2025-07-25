using PrintO.Models.Products.Figurine;
using Zorro.Data;
using Zorro.Query;

namespace PrintO.Query;

public static class GenerateUniqueFigurineVariationSKUQUery
{
    public static QueryContext GenerateUniqueFigurineVariationSKU(
        this QueryContext context,
        out string separateSKU,
        int productId
    )
    {
        var fRepo = context.GetService<ModelRepository<FigurineReference>>()!;
        var fvRepo = context.GetService<ModelRepository<FigurineVariation>>()!;

        IDictionary<string, bool?> c = new Dictionary<string, bool?>
        {
            { "INCLUDE_VARIATIONS", true }
        };
        var figurine = fRepo.FindById(productId, c)!;

        if (figurine.variations.Count == 0)
        {
            separateSKU = figurine.product.SKU;
            return context;
        }
        else
        {
            int variationCounter = 1;
            string newSeparateSKU = string.Empty;
            do
            {
                newSeparateSKU = $"{figurine.product.SKU}.{variationCounter++}";
            }
            while (fvRepo.Find(v => v.separateSKU == newSeparateSKU) is not null);

            separateSKU = newSeparateSKU;
            return context;
        }
    }
}