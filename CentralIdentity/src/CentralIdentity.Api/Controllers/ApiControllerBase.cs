using Microsoft.AspNetCore.Mvc;

namespace CentralIdentity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
}
