using Microsoft.AspNetCore.Authorization;
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
public class StoreController : Controller
{
    [HttpGet]
    [Route("/stores/")]
    [AllowAnonymous]
    public IActionResult GetAllStores(int startIndex, int pageSize = 8)
    {
        return this.StartQuery()

        .GetAll<Store>()

        .MapToDTOs<Store, StoreDTO>()

        .Paginate(startIndex, pageSize)

        .EndAndReturn();
    }

    [HttpGet]
    [Route("/stores/{id}")]
    [AllowAnonymous]
    public IActionResult GetStoreById(int id)
    {
        return this.StartQuery()

        .SetInclusion("INCLUDE_MEMBERS")

        .FindById<Store>(id)

        .MapToDTO<Store, StoreDTO>()

        .EndAndReturn();
    }

    [HttpPost]
    [Route("/stores/")]
    [AllowAnonymous]
    public IActionResult PostStore([FromBody] AddForm input)
    {
        return this.StartQuery()

        .Eject(GetUserQuery.GetUser<User, int>, out User me)

        .If(me.isAdmin is false, _ => _
            .Throw(new (statusCode: StatusCodes.Status403Forbidden))
        )

        .TryEject(_ => _.Find<Store>(s => s.name == input.name), out var sameNameStore)

        .If(sameNameStore is not null, _ => _
            .Throw(new (statusCode: StatusCodes.Status400BadRequest))
        )

        .Eject(_ => _.Add<Store, AddForm>(input), out var newStore)

        .Eject(new UserJoinForm(me), out var joinForm)

        .Update<Store, UserJoinForm>(newStore.Id, joinForm)

        .MapToDTO<Store, StoreDTO>()

        .EndAndReturn();
    }

    [HttpPost]
    [Route("/stores/{id}/members/")]
    public IActionResult PostAddStoreMember(int id, int userId)
    {
        return this.StartQuery()

        .Eject(GetUserQuery.GetUser<User, int>, out User me)

        .If(me.isAdmin is false, _ => _
            .Throw(new (statusCode: StatusCodes.Status403Forbidden))
        )

        .SetInclusion("INCLUDE_MEMBERS")

        .Eject(_ => _.FindById<Store>(id), out Store store)

        .If(store.members.Any(m => m.Id == me.Id) is false, _ => _
            .Throw(new(statusCode: StatusCodes.Status403Forbidden))
        )

        .If(store.members.Any(m => m.Id == userId), _ => _
            .Throw(new (statusCode: StatusCodes.Status400BadRequest))
        )

        .Eject(_ => _.Find<User>(u => u.Id == userId), out User inviteUser)

        .Update<Store, UserJoinForm>(id, new UserJoinForm(inviteUser))

        .MapToDTO<Store, StoreDTO>()

        .EndAndReturn();
    }
}