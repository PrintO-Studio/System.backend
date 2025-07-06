using Microsoft.AspNetCore.Mvc;
using PrintO.Intergrations;
using PrintO.Models.Products.Figurine;
using Zorro.Modules.JwtBearer.Attributes;

namespace PrintO.Controllers.Integrations;

[ApiController]
[JwtAuthorize]
public class OzonFigurineIntegrationController : IntegrationController<OzonIntegration, FigurineReference, FigurineVariation>
{
    public OzonFigurineIntegrationController(OzonIntegration integration) : base(integration)
    {
    }

    [HttpGet]
    [Route("integrations/ozon/listing")]
    public IActionResult GetProductListing()
    {
        return Ok(_integration.GetProductSKUListing());
    }

    [HttpPost]
    [Route("integrations/ozon/products/")]
    public IActionResult PostProducts(string[] SKUs)
    {
        return Ok();
    }

    /*
    [HttpPost]
    [Route("integrations/ozon/pull-sheet-products")]
    public IActionResult PostPullSheetProducts (string[] SKUs)
    {
        _integration.PullSheetData();

        return Ok();
    }
    */
}