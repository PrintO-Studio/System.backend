using PrintO.Interfaces;
using PrintO.Models;
using System.Text.Json;

namespace PrintO.Intergrations.Interfaces;

public interface IIntegradable<TProductReference, TVariation>
    where TProductReference : class, IProductReference<TVariation>
    where TVariation : class, ISellable
{
    public bool HasFigurine(string SKU, out JsonElement root);

    public bool UpdateFigurine(User executor, TProductReference product);

    public bool UpdateAllFigurines(User executor, int storeId);
}