using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel;
using PrintO.Models;
using PrintO.Models.Products;
using PrintO.Models.Products.Figurine;
using PrintO.Query;
using Zorro.Data;
using Zorro.Middlewares;
using Zorro.Modules.JwtBearer.Attributes;
using Zorro.Query;
using Zorro.Query.Essentials;
using Zorro.Query.Essentials.BucketRepository;
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

        .SetInclusion("INCLUDE_NOTES")

        .SetInclusion("INCLUDE_TAGS")

        .Find<FigurineReference>(f => f.product.storeId == selectedStoreId && f.productId == productId)

        .MapToDTO<FigurineReference, object>()

        .EndAndReturn();
    }

    [HttpPost]
    [Route("products/figurines")]
    public IActionResult PostFigurine([FromBody] UserAddForm input)
    {
        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .GetAll<Product>(p => p.storeId == selectedStoreId && p.SKU == input.product.SKU)

        .If(p => p?.Any() ?? false, _ => _
            .Throw(new (
                statusCode: StatusCodes.Status400BadRequest,
                fields: ("SKU", ["Product with such SKU already exist."])))
        )

        .SetInclusion("INCLUDE_VARIATIONS")

        .GetAll<FigurineReference>(f => f.product.storeId == selectedStoreId && f.variations.Any(v => v.separateSKU == input.product.SKU))

        .If(f => f?.Any() ?? false, _ => _
            .Throw(new (
                statusCode: StatusCodes.Status400BadRequest,
                fields: ("SKU", ["Figurine variation with such SKU already exist."])))
        )

        .Eject(_ => _.Add<Product, Product.AddForm>(new Product.AddForm(input.product, selectedStoreId)), out Product product)

        .Eject(new FigurineReference.AddForm(product.Id), out var figurineAdd)

        .Eject(_ => _.Add<FigurineReference, FigurineReference.AddForm>(figurineAdd), out var figurine)

        .ForEach(input.variations, (variationAdd, _) => _
            .GenerateUniqueFigurineVariationSKU(out var separateSKU, product.Id)

            .Eject(new FigurineVariation.AddForm(variationAdd, figurine.Id, separateSKU), out var variatonAdd)

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
                    .Throw(new (statusCode: StatusCodes.Status403Forbidden))
                )

                .SwitchTo(variation)

                .Update(variationUpdate)
            )
        )

        .ForEach(input.addVariations, (variationAdd, _) => _
            .Try(_ => _
                .GenerateUniqueFigurineVariationSKU(out var separateSKU, productId)

                .Eject(new FigurineVariation.AddForm(variationAdd, figurine.Id, separateSKU), out var variatonAdd)

                .Add<FigurineVariation, FigurineVariation.AddForm>(variatonAdd)
            )
        )

        .ForEach(input.removeVariations, (removeVariationId, _) => _
            .Try(_ => _
                .Eject(_ => _.FindById<FigurineVariation>(removeVariationId), out var variation)

                .If(variation.figurineId != figurine.Id, _ => _
                    .Throw(new (statusCode: StatusCodes.Status403Forbidden))
                )!

                .Remove<FigurineVariation>(removeVariationId)
            )
        )

        .End();
    }

    [HttpDelete]
    [Route("products/{productId}/figurines")]
    public IActionResult DeleteFigurine(int productId)
    {
        return this.StartQuery()

        .CheckStoreMembership(out int selectedStoreId)

        .Eject(_ => _.Find<FigurineReference>(f => f.product.storeId == selectedStoreId && f.productId == productId), out var figurine)

        .Eject(_ => _.GetAll<ImageReference>(i => i.productId == productId), out var images)
        .Eject(images.ToArray(), out var imageArray)
        .ForEach(imageArray, (image, _) => _
            .Remove<ImageReference>(image.Id)
        )

        .Eject(_ => _.GetAll<Models.File>(i => i.productId == productId), out var files)
        .Eject(files.ToArray(), out var fileArray)
        .ForEach(fileArray, (file, _) => _
            .Delete<MinIORepository, IMinioClient, Bucket, Item>(file.filePath)

            .Remove<Models.File>(file.Id)
        )

        .Eject(_ => _.GetAll<FigurineVariation>(i => i.figurineId == figurine.Id), out var variations)
        .Eject(variations.ToArray(), out var variationArray)
        .ForEach(variationArray, (variation, _) => _
            .Remove<FigurineVariation>(variation.Id)
        )

        .Remove<FigurineReference>(figurine.Id)

        .Remove<Product>(productId)

        .EndAndDirectTo("/products");
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