using Microsoft.AspNetCore.Mvc;
using PrintO.Intergrations;
using PrintO.Models.Products;
using PrintO.Models.Products.Figurine;
using PrintO.Query;
using Zorro.Data;
using Zorro.Modules.JwtBearer.Attributes;
using Zorro.Query;
using Zorro.Query.Essentials;
using Zorro.Query.Essentials.ModelRepository;

namespace PrintO.Controllers.Integrations;

[ApiController]
[JwtAuthorize]
public class OzonFigurineIntegrationController : IntegrationController<OzonIntegration, FigurineReference, FigurineVariation>
{
    public OzonFigurineIntegrationController(OzonIntegration integration) : base(integration)
    {
    }

    [HttpPost]
    [Route("integrations/ozon/figurines/{id}/upload")]
    public override IActionResult PostUploadFigurine(int id)
    {
        var minIORepo = HttpContext.RequestServices.GetService<MinIORepository>()!;

        return this.StartQuery()

        .CheckStoreMembership(out var selectedStoreId)

        .SetInclusion("INCLUDE_FILES")

        .SetInclusion("INCLUDE_IMAGES")

        .SetInclusion("INCLUDE_VARIATIONS")

        .Eject(_ => _.Find<FigurineReference>(f => f.Id == id && f.product.storeId == selectedStoreId), out var figurine)

        .Eject((long)_integration.UploadFigurine(figurine), out var ozonTaskId)

        .Update<Product, Product.OzonVersionBumpUpdate>(figurine.productId, new())

        .MapToDTO<Product, Product.ProductReviewDTO>(new { minIORepo })

        .EndAndReturn();
    }

    /*
    [HttpPost]
    [Route("integrations/ozon/upload")]
    public IActionResult UploadProducts()
    {
        return this.StartQuery()

        .CheckStoreMembership(out var selectedStoreId)

        .Execute(() => _integration.UploadProductsAsync().GetAwaiter().GetResult())

        .End();
    }
    */
}