using Microsoft.AspNetCore.Mvc;
using PrintO.Intergrations;
using PrintO.Models;
using PrintO.Models.Products;
using PrintO.Models.Products.Figurine;
using PrintO.Query;
using Zorro.Data;
using Zorro.Modules.JwtBearer.Attributes;
using Zorro.Query;
using Zorro.Query.Essentials;
using Zorro.Query.Essentials.Auth;
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
    [Route("integrations/ozon/figurines/update")]
    public override IActionResult PostUpdateAllFigurines()
    {
        var minIORepo = HttpContext.RequestServices.GetService<MinIORepository>()!;

        return this.StartQuery()

        .Eject(GetUserQuery.GetUser<User, int>, out var me)

        .CheckStoreMembership(out var selectedStoreId)

        .Execute(() => _integration.UpdateAllFigurines(me, selectedStoreId))

        .End();
    }

    [HttpPost]
    [Route("integrations/ozon/figurines/{id}/update")]
    public override IActionResult PostUpdateFigurine(int id)
    {
        var minIORepo = HttpContext.RequestServices.GetService<MinIORepository>()!;

        return this.StartQuery()

        .Eject(GetUserQuery.GetUser<User, int>, out var me)

        .CheckStoreMembership(out var selectedStoreId)

        .SetInclusion("INCLUDE_FILES")

        .SetInclusion("INCLUDE_IMAGES")

        .SetInclusion("INCLUDE_VARIATIONS")

        .Eject(_ => _.Find<FigurineReference>(f => f.Id == id && f.product.storeId == selectedStoreId), out var figurine)

        .Execute(() => _integration.UpdateFigurine(me, figurine))

        .FindById<Product>(figurine.productId)

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