using Autofac;
using Autofac.Extensions.DependencyInjection;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.Models.Infrastructure;
using eShopLegacyMVC.Modules;
using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;

// Configure log4net
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly()!);
XmlConfigurator.Configure(logRepository, new FileInfo("log4Net.xml"));

var builder = WebApplication.CreateBuilder(args);

// Use Autofac as the DI container
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// Connection strings
var catalogConnectionString = builder.Configuration.GetConnectionString("CatalogDBContext");
var identityConnectionString = builder.Configuration.GetConnectionString("IdentityDBContext");
var useMockData = builder.Configuration.GetValue<bool>("AppSettings:UseMockData");

// Add DbContext for catalog
builder.Services.AddDbContext<CatalogDBContext>(options =>
    options.UseSqlServer(catalogConnectionString));

// Add DbContext for Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(identityConnectionString));

// Add ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
});

// Add MVC with views
builder.Services.AddControllersWithViews();

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// Add HTTP context accessor (needed by WebHelper)
builder.Services.AddHttpContextAccessor();

// Register WebHelper as a scoped service
builder.Services.AddScoped<eShopLegacy.Utilities.WebHelper>();

// Configure Autofac modules for catalog services
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterModule(new ApplicationModule(useMockData));
});

var app = builder.Build();

// Configure middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Shared/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Session initialization middleware (equivalent to Session_Start in Global.asax)
app.Use(async (context, next) =>
{
    await context.Session.LoadAsync();
    if (context.Session.GetString("MachineName") == null)
    {
        context.Session.SetString("MachineName", Environment.MachineName);
        context.Session.SetString("SessionStartTime", DateTime.Now.ToString());
    }
    await next(context);
});

app.UseAuthentication();
app.UseAuthorization();

// Initialize the database (equivalent to ConfigDataBase in Global.asax)
if (!useMockData)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<CatalogDBContext>();
        var contentRootPath = app.Environment.ContentRootPath;
        var useCustomizationData = builder.Configuration.GetValue<bool>("AppSettings:UseCustomizationData");
        CatalogDBInitializer.Initialize(dbContext, contentRootPath, useCustomizationData);
    }
    catch (Exception ex)
    {
        var log = LogManager.GetLogger(typeof(Program));
        log.Error("An error occurred while seeding the database.", ex);
    }
}

// Map attribute-routed and conventional routes
app.MapControllerRoute(
    name: "GetPicRouteTemplate",
    pattern: "items/{catalogItemId:int}/pic",
    defaults: new { controller = "Pic", action = "Index" });

app.MapControllerRoute(
    name: "api",
    pattern: "api/{controller}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

app.Run();
