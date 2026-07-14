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
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure log4net
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly()!);
var log4netConfigFile = new FileInfo(Path.Combine(builder.Environment.ContentRootPath, "log4Net.xml"));
if (log4netConfigFile.Exists)
{
    XmlConfigurator.Configure(logRepository, log4netConfigFile);
}

// Use Autofac as the DI container
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add EF Core DbContexts
var catalogConnectionString = builder.Configuration.GetConnectionString("CatalogDBContext")
    ?? "Data Source=(localdb)\\MSSQLLocalDB; Initial Catalog=Microsoft.eShopOnContainers.Services.CatalogDb; Integrated Security=True; MultipleActiveResultSets=True;";
var identityConnectionString = builder.Configuration.GetConnectionString("IdentityDBContext")
    ?? "Data Source=(LocalDb)\\MSSQLLocalDB; Initial Catalog=Microsoft.eShopOnContainers.Services.IdentityDb; Integrated Security=True";

builder.Services.AddDbContext<CatalogDBContext>(options =>
    options.UseSqlServer(catalogConnectionString));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(identityConnectionString));

// Add ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
});

// Register CatalogItemHiLoGenerator as singleton
builder.Services.AddSingleton<CatalogItemHiLoGenerator>();

// Register WebHelper
builder.Services.AddSingleton<eShopLegacy.Utilities.WebHelper>();

// Configure Autofac
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    var useMockData = bool.Parse(builder.Configuration["UseMockData"] ?? "true");
    containerBuilder.RegisterModule(new ApplicationModule(useMockData));
});

var app = builder.Build();

// Configure WebHelper static accessor
var httpContextAccessor = app.Services.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
eShopLegacy.Utilities.WebHelper.Configure(httpContextAccessor);

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var useMockData = bool.Parse(app.Configuration["UseMockData"] ?? "true");
        if (!useMockData)
        {
            var catalogDb = services.GetRequiredService<CatalogDBContext>();
            var hiLoGenerator = services.GetRequiredService<CatalogItemHiLoGenerator>();
            var config = services.GetRequiredService<IConfiguration>();
            var env = services.GetRequiredService<IWebHostEnvironment>();
            var initializer = new CatalogDBInitializer(hiLoGenerator, config, env.ContentRootPath);
            initializer.Seed(catalogDb);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Shared/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serve static files from the content root (legacy location)
app.UseStaticFiles(); // wwwroot
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Content")),
    RequestPath = "/Content"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Scripts")),
    RequestPath = "/Scripts"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Images")),
    RequestPath = "/Images"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Pics")),
    RequestPath = "/Pics"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "fonts")),
    RequestPath = "/fonts"
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
