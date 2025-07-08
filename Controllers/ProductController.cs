using Microsoft.AspNetCore.Mvc;
using PrintO.Models.Products;
using PrintO.Query;
using Zorro.Data;
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
        var minIORepo = HttpContext.RequestServices.GetService<MinIORepository>()!;

        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .SetInclusion("INCLUDE_FILES")

        .SetInclusion("INCLUDE_IMAGES")

        .Eject(Array.Empty<Product>(), out IEnumerable<Product> qualifiedProducts)
        
        .If(string.IsNullOrEmpty(searchQuery), _ => _
             .Eject(_ => _.GetAllWhere<Product>(p => p.storeId == selectedStoreId), out var products)

             .Execute(() => qualifiedProducts = products)
        )!

        .If(!string.IsNullOrEmpty(searchQuery), _ => _
             .Eject(_ => _.GetAllWhere<Product>(p => p.storeId == selectedStoreId && (p.name.Contains(searchQuery!) || p.SKU.Contains(searchQuery!))), out var products)

             .Execute(() => qualifiedProducts = products)
        )

        .SwitchTo(qualifiedProducts)

        .MapToDTOs<Product, Product.ProductReviewDTO>(p => new { minIORepo })

        .PaginateAndWrap(startIndex, pageSize, true)

        .EndAndReturn();
    }

    /*
    [HttpGet]
    [Route("/products/{id}")]
    public IActionResult GetProduct(int id)
    {
        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .Find<Product>(f => f.storeId == selectedStoreId && f.Id == id)

        .MapToDTO<Product, object>()

        .EndAndReturn();
    }

    [HttpPost]
    [Route("/products")]
    public IActionResult PostProduct([FromBody] Product.UserAddForm input)
    {
        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .GetAllWhere<Product>(p => p.storeId == selectedStoreId && p.SKU == input.SKU)

        .If(p => p?.Any() ?? false, _ => _
            .Throw(new QueryException(
                statusCode: StatusCodes.Status400BadRequest,
                fields: ("SKU", ["Product with such SKU already exist."])))
        )

        .Add<Product, Product.AddForm>(new(input, selectedStoreId))

        .MapToDTO<Product, object>()

        .EndAndReturn();
    }

    [HttpPatch]
    [Route("/products/{id}")]
    public IActionResult PatchProduct(int id, [FromBody] Product.UpdateForm input)
    {
        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .Find<Product>(p => p.Id == id && p.storeId == selectedStoreId)

        .Update(input)

        .End();
    }
    */
}