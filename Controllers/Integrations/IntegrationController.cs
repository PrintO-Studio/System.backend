using Microsoft.AspNetCore.Mvc;
using PrintO.Interfaces;
using PrintO.Intergrations.Interfaces;
using Zorro.Modules.JwtBearer.Attributes;

namespace PrintO.Controllers.Integrations;

[ApiController]
[JwtAuthorize]
public abstract class IntegrationController<TIntegradable, TProductReference, TVariation> : Controller
    where TIntegradable : IIntegradable<TProductReference, TVariation>
    where TProductReference : class, IProductReference<TVariation>
    where TVariation : class, ISellable
{
    protected readonly TIntegradable _integration;

    public IntegrationController(TIntegradable integration)
    {
        _integration = integration;
    }

    public abstract IActionResult PostUploadFigurine(int id);
}