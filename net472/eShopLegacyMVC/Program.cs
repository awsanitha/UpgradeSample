using Autofac;
using Autofac.Extensions.DependencyInjection;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.Models.Infrastructure;
using eShopLegacyMVC.Modules;
using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

// Configure log4net
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly()!);
XmlConfigurator.Configure(logRepository, new FileInfo("log4Net.xml"));

var builder = WebApplication.CreateBuilder(args);

// Use Autofac as DI container
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// Configure services
var connectionString = builder.Configuration.GetConnectionString("CatalogDBContext");
var identityConnectionString = builder.Configuration.GetConnectionString("IdentityDBContext");

// EF Core - Catalog DB
builder.Services.AddDbContext<CatalogDBContext>(options =>
    options.UseSqlServer(connectionString));

// EF Core - Identity DB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(identityConnectionString));

// ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure cookie authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Add MVC with Razor views
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

// Add session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add HttpContextAccessor (used by WebHelper)
builder.Services.AddHttpContextAccessor();

// Register IConfiguration for CatalogDBInitializer
builder.Services.AddSingleton(builder.Configuration);

// Configure Autofac container
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    var useMockData = bool.Parse(builder.Configuration["UseMockData"] ?? "false");
    containerBuilder.RegisterModule(new ApplicationModule(useMockData));

    // Register WebHelper
    containerBuilder.RegisterType<eShopLegacy.Utilities.WebHelper>()
        .AsSelf()
        .InstancePerLifetimeScope();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Shared/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Map attribute routes and conventional routes
app.MapControllerRoute(
    name: "GetPicRouteTemplate",
    pattern: "items/{catalogItemId:int}/pic",
    defaults: new { controller = "Pic", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

app.MapControllers();

// Seed database on startup (non-mock mode)
if (!bool.Parse(app.Configuration["UseMockData"] ?? "false"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDBContext>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

    try
    {
        db.Database.EnsureCreated();
        if (!db.CatalogItems.Any())
        {
            var hiLoGenerator = scope.ServiceProvider.GetRequiredService<eShopLegacyMVC.Models.CatalogItemHiLoGenerator>();
            var initializer = new CatalogDBInitializer(hiLoGenerator, configuration, env.ContentRootPath);
            initializer.Seed(db);
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
