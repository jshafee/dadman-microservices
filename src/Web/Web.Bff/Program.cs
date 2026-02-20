using System.Security.Claims;
using BuildingBlocks.ServiceDefaults;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Web.Bff;

var builder = WebApplication.CreateBuilder(args);
var ticketStore = new InMemoryTicketStore();

builder.Services.AddServiceDefaults();
builder.Services.AddSingleton<ITicketStore>(ticketStore);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "dadman_bff";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SessionStore = ticketStore;
    });
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "dadman_csrf";
    options.Cookie.HttpOnly = false;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

app.UseServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/bff/antiforgery", (HttpContext context, IAntiforgery antiforgery) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    return Results.Ok(new { token = tokens.RequestToken });
});

app.MapPost("/bff/dev/login", async (HttpContext context) =>
{
    if (!app.Environment.IsDevelopment())
    {
        return Results.NotFound();
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "dev-user"),
        new Claim(ClaimTypes.Name, "Dev User"),
        new Claim("role", "developer")
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

    return Results.Ok(new { message = "logged in" });
});

app.MapPost("/bff/logout", async (HttpContext context, IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(context);
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok(new { message = "logged out" });
});

app.MapGet("/bff/me", (ClaimsPrincipal user) =>
{
    if (user.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        name = user.Identity?.Name,
        claims = user.Claims.Select(c => new { c.Type, c.Value })
    });
});

app.MapPost("/bff/echo", async (HttpContext context, IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(context);
    return Results.Ok(new { message = "state change accepted" });
});

app.MapFallbackToFile("index.html");

app.Run();
