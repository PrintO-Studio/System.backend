using PrintO.Models.Products;

namespace PrintO.Interfaces;

public interface IProductReference<TVariation>
    where TVariation : class, ISellable
{
    public Product product { get; set; }
    public abstract ICollection<TVariation> variations { get; set; }
}