using Microsoft.AspNetCore.Mvc;
using PrintO.Models;
using Zorro.Middlewares;
using Zorro.Modules.JwtBearer.Attributes;
using Zorro.Query;
using Zorro.Query.Essentials;
using Zorro.Query.Essentials.Auth;
using Zorro.Query.Essentials.ModelRepository;
using static PrintO.Models.Store;

namespace PrintO.Controllers;

[ApiController]
[JwtAuthorize]
public class UserController : Controller
{
    [HttpGet]
    [Route("/users/me/memberships")]
    public IActionResult GetMyMemberships()
    {
        return this.StartQuery()

        .SetInclusion("INCLUDE_MEMBERS")

        .Eject(GetUserQuery.GetUser<User, int>, out var me)

        .GetAllWhere<Store>(s => s.members.Any(m => m.Id == me.Id))

        .MapToDTOs<Store, StoreDTO>()

        .EndAndReturn();
    }

    [HttpPost]
    [Route("/users/me/memberships/{id}/select")]
    public IActionResult PostSelectStore(int id)
    {
        return this.StartQuery()

        .Eject(GetUserQuery.GetUser<User, int>, out var me)

        .SetInclusion("INCLUDE_MEMBERS")

        .Eject(_ => _.FindById<Store>(id), out var store)

        .If(store.members.Any(m => m.Id == me.Id) is false, _ => _
            .Throw(new QueryException(statusCode: StatusCodes.Status403Forbidden))
        )

        .SwitchTo(me)

        .Update(new User.SelectStoreForm(id))

        .EndAndDirectTo("/products");
    }
}