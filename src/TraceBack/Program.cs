using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TraceBack.Common;
using TraceBack.Data;
using TraceBack.Services;

// 1. Load environment variables from .env file (if present)
EnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

// 2. Register MVC Controllers and Views
builder.Services.AddControllersWithViews();

// 3. Register Data Store and Application Services
builder.Services.AddSingleton<MongoStore>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<ItemService>();
builder.Services.AddScoped<IItemService>(provider => provider.GetRequiredService<ItemService>());
builder.Services.AddScoped<Func<IItemService>>(provider => () => provider.GetRequiredService<IItemService>());
builder.Services.AddSingleton<R2ImageStorage>();
builder.Services.AddSingleton<IImageStorage>(provider => provider.GetRequiredService<R2ImageStorage>());
builder.Services.AddSingleton<IItemImageRepository, MongoItemImageRepository>();
builder.Services.AddSingleton<ItemImageService>();
builder.Services.AddScoped<ClaimService>();
builder.Services.AddScoped<AdminBootstrapService>();

// 4. Configure JWT Settings and Authentication
var secret = builder.Configuration["Jwt:Secret"]
    ?? builder.Configuration["Jwt__Secret"]
    ?? builder.Configuration["JWT_SECRET"];

if (string.IsNullOrWhiteSpace(secret))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Set Jwt__Secret before starting the application.");
    }

    // Ephemeral signing key for development if no secret is supplied
    secret = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
}

if (Encoding.UTF8.GetByteCount(secret) < 32)
{
    throw new InvalidOperationException("Jwt:Secret must contain at least 32 UTF-8 bytes.");
}

var issuer = builder.Configuration["Jwt:Issuer"]
    ?? builder.Configuration["Jwt__Issuer"]
    ?? builder.Configuration["JWT_ISSUER"]
    ?? "TraceBack";

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
builder.Services.AddSingleton(new JwtSettings(key, issuer));

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
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = "TraceBack.Api",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// 5. Build Application
var app = builder.Build();

// 6. Bootstrap Initial Admin User (if configured)
var bootstrapEmail = builder.Configuration["BootstrapAdmin:Email"]
    ?? builder.Configuration["BootstrapAdmin__Email"]
    ?? builder.Configuration["BOOTSTRAP_ADMIN_EMAIL"];

var bootstrapPassword = builder.Configuration["BootstrapAdmin:Password"]
    ?? builder.Configuration["BootstrapAdmin__Password"]
    ?? builder.Configuration["BOOTSTRAP_ADMIN_PASSWORD"];

if (!string.IsNullOrWhiteSpace(bootstrapEmail) || !string.IsNullOrWhiteSpace(bootstrapPassword))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AdminBootstrapService>().EnsureAdminAsync();
}

// 7. Configure HTTP Request Pipeline
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
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
