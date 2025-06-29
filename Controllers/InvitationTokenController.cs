using Microsoft.AspNetCore.Mvc;
using PrintO.Models;
using Zorro.Modules.JwtBearer.Attributes;
using Zorro.Query;
using Zorro.Query.Essentials;
using Zorro.Query.Essentials.Auth;
using Zorro.Query.Essentials.ModelRepository;

namespace PrintO.Controllers;

[ApiController]
[JwtAuthorize]
public class InvitationTokenController : Controller
{
    [HttpPost]
    [Route("/tokens/")]
    public IActionResult PostInvitationToken()
    {
        return this.StartQuery()

        .Eject(GetUserQuery.GetUser<User, int>, out User me)

        .If(me.isAdmin is false, _ => _
            .Throw(new(statusCode: StatusCodes.Status403Forbidden))
        )

        .Eject(new InvitationToken.AddForm(me.Id), out var addForm)

        .Add<InvitationToken, InvitationToken.AddForm>(addForm)

        .MapToDTO<InvitationToken, object>()

        .EndAndReturn();
    }
}