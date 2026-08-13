using Autofac;
using Autofac.Extensions.DependencyInjection;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
var configuration = builder.Configuration;

// --- Autofac integration ---
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// --- Services ---
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

// --- Session ---
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

// --- EF Core - Catalog DB ---
var catalogConnectionString = configuration.GetConnectionString("CatalogDBContext")
    ?? "Data Source=(localdb)\\MSSQLLocalDB; Initial Catalog=Microsoft.eShopOnContainers.Services.CatalogDb; Integrated Security=True; MultipleActiveResultSets=True;";
builder.Services.AddDbContext<CatalogDBContext>(options =>
    options.UseSqlServer(catalogConnectionString));

// --- EF Core - Identity DB ---
var identityConnectionString = configuration.GetConnectionString("IdentityDBContext")
    ?? "Data Source=(LocalDb)\\MSSQLLocalDB; Initial Catalog=Microsoft.eShopOnContainers.Services.IdentityDb; Integrated Security=True";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(identityConnectionString));

// --- ASP.NET Core Identity ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.SlidingExpiration = true;
});

// --- Logging ---
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// --- Autofac container modules ---
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    var useMockData = bool.Parse(configuration["UseMockData"] ?? "false");
    containerBuilder.RegisterModule(new ApplicationModule(useMockData));
});

var app = builder.Build();

// --- Middleware pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Shared/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

// Attribute routing for controllers
app.MapControllers();

// Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

// --- Seed database on startup if not using mock data ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var useMockData = bool.Parse(configuration["UseMockData"] ?? "false");
        if (!useMockData)
        {
            var catalogContext = services.GetRequiredService<CatalogDBContext>();
            // Ensure the database is created but don't auto-seed in production
            // The CatalogDBInitializer can be invoked separately if needed
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during application startup.");
    }
}

app.Run();
