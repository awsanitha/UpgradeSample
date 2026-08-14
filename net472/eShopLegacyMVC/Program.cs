using eShopLegacyMVC.Models;
using eShopLegacyMVC.Models.Infrastructure;
using eShopLegacyMVC.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ────────────────────────────────────────────────────────────
var configuration = builder.Configuration;
var useMockData = bool.Parse(configuration["AppSettings:UseMockData"] ?? "false");

// ── Logging ──────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ── Services ─────────────────────────────────────────────────────────────────

// EF Core – Catalog
if (!useMockData)
{
    builder.Services.AddDbContext<CatalogDBContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("CatalogDBContext")));
}

// EF Core – Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("IdentityDBContext")));

// ASP.NET Core Identity
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 6;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.AllowedForNewUsers = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// MVC + Razor runtime compilation
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// HTTP context accessor (for helpers that need it)
builder.Services.AddHttpContextAccessor();

// Catalog service
if (useMockData)
{
    builder.Services.AddSingleton<ICatalogService, CatalogServiceMock>();
}
else
{
    builder.Services.AddScoped<ICatalogService, CatalogService>();
    builder.Services.AddScoped<CatalogDBInitializer>();
}

builder.Services.AddSingleton<CatalogItemHiLoGenerator>();

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Initialize database ───────────────────────────────────────────────────────
if (!useMockData)
{
    using (var scope = app.Services.CreateScope())
    {
        var initializer = scope.ServiceProvider.GetRequiredService<CatalogDBInitializer>();
        initializer.Initialize(scope.ServiceProvider);
    }
}

// ── Pipeline ──────────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Serve static files from the project root directories
app.UseStaticFiles(); // wwwroot

// Also serve Content, Scripts, Images, Pics, fonts from content root
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(builder.Environment.ContentRootPath),
    RequestPath = ""
});

app.UseRouting();
app.UseSession();

// Populate session on first access (replaces Session_Start)
app.Use(async (context, next) =>
{
    await context.Session.LoadAsync();
    if (string.IsNullOrEmpty(context.Session.GetString("MachineName")))
    {
        context.Session.SetString("MachineName", Environment.MachineName);
        context.Session.SetString("SessionStartTime", DateTime.Now.ToString("g"));
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// Attribute routes
app.MapControllers();

// Convention route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

app.Run();
