namespace PrintO.Interfaces;

public interface ISellable
{
    public ulong priceRub { get; set; }
    public ulong priceBeforeSaleRub { get; set; }
    public ulong minimalPriceRub { get; set; }
}