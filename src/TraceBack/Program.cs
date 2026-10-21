using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TraceBack.Data;
using TraceBack.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<MongoStore>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<ItemService>();
builder.Services.AddScoped<ClaimService>();
builder.Services.AddScoped<AdminBootstrapService>();

var secret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(secret))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Set Jwt__Secret before starting the application.");
    secret = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
}
if (Encoding.UTF8.GetByteCount(secret) < 32)
    throw new InvalidOperationException("Jwt:Secret must contain at least 32 UTF-8 bytes.");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
builder.Services.AddSingleton(new JwtSettings(key, builder.Configuration["Jwt:Issuer"] ?? "TraceBack"));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Denied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "TraceBack",
            ValidateAudience = true,
            ValidAudience = "TraceBack.Api",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
if (!string.IsNullOrWhiteSpace(builder.Configuration["BootstrapAdmin:Email"]) ||
    !string.IsNullOrWhiteSpace(builder.Configuration["BootstrapAdmin:Password"]))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AdminBootstrapService>().EnsureAdminAsync();
}
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();

public partial class Program { }
