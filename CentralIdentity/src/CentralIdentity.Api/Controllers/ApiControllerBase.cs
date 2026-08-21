using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace CentralIdentity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected long? CurrentUserId
    {
        get
        {
            var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            return long.TryParse(subject, out var userId) ? userId : null;
        }
    }

    protected bool IsAdministrator =>
        User.IsInRole("admin") ||
        User.IsInRole("administrator");

    protected bool CanAccessUser(long userId) =>
        CurrentUserId == userId || IsAdministrator;
}
