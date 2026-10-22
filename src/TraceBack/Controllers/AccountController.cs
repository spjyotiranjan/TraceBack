using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Controllers;

public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromServices] AccountService accounts, LoginForm form, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var user = await accounts.LoginAsync(form);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "The email or password is incorrect.");
            return View(form);
        }

        await SignInAsync(user);
        TempData["Success"] = $"Welcome back, {user.Name}.";

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action("Index", "Dashboard")!);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register([FromServices] AccountService accounts, RegisterForm form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var user = await accounts.RegisterAsync(form);
        if (user is null)
        {
            ModelState.AddModelError(nameof(form.Email), "An account with this email already exists.");
            return View(form);
        }

        await SignInAsync(user);
        TempData["Success"] = "Your account is ready.";

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Denied()
    {
        return View();
    }

    private Task SignInAsync(AppUser user)
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, user.Id),
            new System.Security.Claims.Claim(ClaimTypes.Name, user.Name),
            new System.Security.Claims.Claim(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}
