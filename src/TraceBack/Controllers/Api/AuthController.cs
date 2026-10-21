using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Controllers.Api;

public sealed class AuthController(AccountService accounts) : ApiControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Name))
        {
            return BadRequest(new { error = "Name is required." });
        }

        var user = await accounts.RegisterAsync(form);
        if (user is null)
        {
            return Conflict(new { error = "An account with this email already exists." });
        }

        var token = accounts.CreateToken(user);
        return Ok(new AuthResponse(token, UserResponse.From(user)));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginForm form)
    {
        var user = await accounts.LoginAsync(form);
        if (user is null)
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }

        var token = accounts.CreateToken(user);
        return Ok(new AuthResponse(token, UserResponse.From(user)));
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            id = UserId,
            name = User.Identity?.Name,
            role = IsAdmin ? "Admin" : "Student"
        });
    }
}
