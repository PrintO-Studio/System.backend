using Microsoft.AspNetCore.Mvc;
using PrintO.Models.Products;
using PrintO.Models.Products.Figurine;
using PrintO.Query;
using Zorro.Data;
using Zorro.Middlewares;
using Zorro.Modules.JwtBearer.Attributes;
using Zorro.Query;
using Zorro.Query.Essentials;
using Zorro.Query.Essentials.ModelRepository;

namespace PrintO;

[ApiController]
[JwtAuthorize]
public class FigurineController : Controller
{
    [HttpGet]
    [Route("products/{productId}/figurine")]
    public IActionResult GetFigurine(int productId)
    {
        var minIORepo = HttpContext.RequestServices.GetService<MinIORepository>()!;

        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .SetInclusion("INCLUDE_FILES")

        .SetInclusion("INCLUDE_IMAGES")

        .SetInclusion("INCLUDE_VARIATIONS")

        .Find<FigurineReference>(f => f.product.storeId == selectedStoreId && f.productId == productId)

        .MapToDTO<FigurineReference, object>(new { minIORepo })

        .EndAndReturn();
    }


    [HttpPost]
    [Route("products/figurines")]
    public IActionResult PostFigurine([FromBody] UserAddForm input)
    {
        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .GetAllWhere<Product>(p => p.storeId == selectedStoreId && p.SKU == input.product.SKU)

        .If(p => p?.Any() ?? false, _ => _
            .Throw(new QueryException(
                statusCode: StatusCodes.Status400BadRequest,
                fields: ("SKU", ["Product with such SKU already exist."])))
        )

        .Eject(_ => _.Add<Product, Product.AddForm>(new(input.product, selectedStoreId)), out Product product)

        .Eject(new FigurineReference.AddForm(product.Id), out var figurineAdd)

        .Eject(_ => _.Add<FigurineReference, FigurineReference.AddForm>(figurineAdd), out var figurine)

        .ForEach(input.variations, (variationAdd, _) => _
            .Eject(new FigurineVariation.AddForm(variationAdd, figurine.Id), out var variatonAdd)

            .Add<FigurineVariation, FigurineVariation.AddForm>(variatonAdd)
        )

        .EndAndDirectTo($"/products/{product.Id}/edit");
    }

    [HttpPatch]
    [Route("products/{productId}/figurines")]
    public IActionResult PatchFigurine(int productId, [FromBody] UserUpdateForm input)
    {
        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .Eject(_ => _.Find<FigurineReference>(f => f.product.storeId == selectedStoreId && f.productId == productId), out var figurine)

        .Update<Product, Product.UpdateForm>(productId, input.product)

        .ForEach(input.updateVariations, (variationUpdate, _) => _
            .Try(_ => _
                .Eject(_ => _.FindById<FigurineVariation>(variationUpdate.id), out var variation)

                .If(variation.figurineId != figurine.Id, _ => _
                    .Throw(new QueryException(statusCode: StatusCodes.Status403Forbidden))
                )!

                .Update<FigurineVariation, FigurineVariation.UpdateForm>(variationUpdate)
            )
        )

        .ForEach(input.addVariations, (variationAdd, _) => _
            .Try(_ => _
                .Eject(new FigurineVariation.AddForm(variationAdd, figurine.Id), out var variatonAdd)

                .Add<FigurineVariation, FigurineVariation.AddForm>(variatonAdd)
            )
        )

        .ForEach(input.removeVariations, (removeVariationId, _) => _
            .Try(_ => _
                .Eject(_ => _.FindById<FigurineVariation>(removeVariationId), out var variation)

                .If(variation.figurineId != figurine.Id, _ => _
                    .Throw(new QueryException(statusCode: StatusCodes.Status403Forbidden))
                )!

                .Remove<FigurineVariation>(removeVariationId)
            )
        )

        .End();
    }

    public struct UserAddForm
    {
        public Product.UserAddForm product { get; set; }
        public FigurineVariation.UserAddForm[] variations { get; set; }
    }

    public struct UserUpdateForm
    {
        public Product.UpdateForm product { get; set; }
        public FigurineVariation.UpdateForm[] updateVariations { get; set; }
        public FigurineVariation.UserAddForm[] addVariations { get; set; }
        public int[] removeVariations { get; set; }
    }
}