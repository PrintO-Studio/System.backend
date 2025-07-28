using Microsoft.AspNetCore.Mvc;
using PrintO.Models.Products;
using PrintO.Query;
using Zorro.Modules.JwtBearer.Attributes;
using Zorro.Query;
using Zorro.Query.Essentials;
using Zorro.Query.Essentials.ModelRepository;

namespace PrintO.Controllers;

[ApiController]
[JwtAuthorize]
public class ProductController : Controller
{
    [HttpGet]
    [Route("/products")]
    public IActionResult GetAllProducts(string? searchQuery, int startIndex, int pageSize = 12)
    {
        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .SetInclusion("INCLUDE_FILES")

        .SetInclusion("INCLUDE_IMAGES")

        .Eject(Array.Empty<Product>(), out IEnumerable<Product> qualifiedProducts)

        .If(string.IsNullOrEmpty(searchQuery), _ => _
             .Eject(_ => _.GetAll<Product>(p => p.storeId == selectedStoreId), out var products)

             .Execute(() => qualifiedProducts = products)
        )

        .If(!string.IsNullOrEmpty(searchQuery), _ => _
             .Eject(_ => _.GetAll<Product>(
                 p => p.storeId == selectedStoreId &&
                 (p.name.Contains(searchQuery!) || p.SKU.Contains(searchQuery!) ||
                    (!string.IsNullOrEmpty(p.series) && p.series!.Contains(searchQuery!)))),
             out var products)

             .Execute(() => qualifiedProducts = products)
        )

        .SwitchTo(qualifiedProducts)

        .Paginate(startIndex, pageSize, true)

        .MapToDTOs<Product, Product.ProductReviewDTO>()

        .EndAndReturn();
    }
}