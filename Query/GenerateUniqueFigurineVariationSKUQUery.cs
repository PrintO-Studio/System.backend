using PrintO.Models.Products.Figurine;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Zorro.Data;
using Zorro.Query;

namespace PrintO.Query;

public static class GenerateUniqueFigurineVariationSKUQUery
{
    public static HttpQueryContext GenerateUniqueFigurineVariationSKU(
        this HttpQueryContext context,
        out string separateSKU,
        int productId
    )
    {
        var fRepo = context.GetService<ModelRepository<FigurineReference>>()!;
        var fvRepo = context.GetService<ModelRepository<FigurineVariation>>()!;

        ConcurrentDictionary<string, bool?> c = new ConcurrentDictionary<string, bool?>();
        c.TryAdd("INCLUDE_VARIATIONS", true);

        var figurine = fRepo.FindById(productId, c)!;

        if (figurine.variations.Count == 0)
        {
            separateSKU = figurine.product.newSKU;
            return context;
        }
        else
        {
            string prefix = Regex.Match(figurine.product.newSKU, @"^\D+").Value;
            int number = int.Parse(Regex.Match(figurine.product.newSKU, @"\d+").Value);

            string newSeparateSKU;

            do
            {
                number++;
                newSeparateSKU = prefix + number;
            }
            while (fvRepo.Find(v => v.separateSKU == newSeparateSKU) is not null);

            separateSKU = newSeparateSKU;
            return context;
        }
    }
}