using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PrintO.Models;
using Zorro.Modules.JwtBearer.Attributes;
using Zorro.Query;
using Zorro.Query.Essentials;
using Zorro.Query.Essentials.Auth;
using Zorro.Query.Essentials.ModelRepository;
using Zorro.Services;
using static PrintO.Models.User;
using static Zorro.Query.Essentials.Auth.SignInUserQuery;
using static Zorro.Query.Essentials.Auth.SignUpUserQuery;

namespace ZorroTest.Controllers;

[JwtAuthorize]
public class JwtAuthController : Controller
{
    [HttpPost]
    [Route("/auth/signIn")]
    [AllowAnonymous]
    public IActionResult PostSignInUser([FromBody] UsernameSingInForm input)
    {
        return this.StartQuery()

        .SignInUser<User, int>(input)

        .MapToDTO<User, UserDTO>()

        .EndAndDirectTo("/products");
    }

    [HttpPost]
    [Route("/auth/signUp")]
    [AllowAnonymous]
    public IActionResult PostSignUpUser([FromBody] UsernameSignUpForm input, string token)
    {
        return this.StartQuery()

        .Eject(_ => _.Find<InvitationToken>(t => t.token == token), out var tokenEntity)

        .If(tokenEntity.used, _ => _
            .Throw(new(statusCode: StatusCodes.Status400BadRequest))
        )

        .SignUpUser<User, int>(input)

        .Eject(out User user)

        .Eject(new InvitationToken.UseForm(user.Id), out var tokenUseForm)

        .Update<InvitationToken, InvitationToken.UseForm>(tokenEntity.Id, tokenUseForm)

        .SwitchTo(user)

        .MapToDTO<User, UserDTO>()

        .EndAndDirectTo("/products");
    }

    [HttpGet]
    [Route("auth/isAuthenticated")]
    [AllowAnonymous]
    public IActionResult GetIsAuthenticated()
    {
        var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
        return new OkObjectResult(new
        {
            isAuthenticated = !string.IsNullOrEmpty(userManager.GetUserId(HttpContext.User))
        });
    }

    [HttpGet]
    [Route("auth/me")]
    public IActionResult GetMe()
    {
        return this.StartQuery()

        .GetUser<User, int>()

        .MapToDTO<User, UserDTO>()

        .EndAndReturn();
    }

    [HttpPost]
    [Route("auth/logOut")]
    public IActionResult PostLogOut()
    {
        if (Request.Cookies.Any(cookie => cookie.Key == JwtBearerService.COOKIE_TOKEN_NAME))
        {
            Response.Cookies.Delete(JwtBearerService.COOKIE_TOKEN_NAME);
            return new OkObjectResult(new { next = "/signIn", success = true });
        }
        else
        {
            return new OkObjectResult(new { success = false });
        }
    }
}
