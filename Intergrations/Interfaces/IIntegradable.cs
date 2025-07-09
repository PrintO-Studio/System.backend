using PrintO.Interfaces;
using System.Text.Json;

namespace PrintO.Intergrations.Interfaces;

public interface IIntegradable<TProductReference, TVariation>
    where TProductReference : class, IProductReference<TVariation>
    where TVariation : class, ISellable
{
    public object UploadFigurine(TProductReference product);
}