using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace TraceBack.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public abstract class ApiControllerBase : ControllerBase
{
    protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    protected bool IsAdmin => User.IsInRole("Admin");

    protected static bool ValidId(string id) => ObjectId.TryParse(id, out _);
}
