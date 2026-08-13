# Migration Summary: .NET Framework 4.7.2 → .NET 10

## Build Status

| Project | Target | Status | Errors | Warnings |
|---------|--------|--------|--------|----------|
| eShopLegacy.Common | net10.0 | ✅ SUCCESS | 0 | 0 |
| eShopLegacy.Utilities | net10.0 | ✅ SUCCESS | 0 | 0 |
| eShopLegacyMVC | net10.0 | ✅ SUCCESS | 0 | 0 |

## Changes Made

### Project Files

**eShopLegacy.Common.csproj**
- Converted from old-style XML to SDK-style `Microsoft.NET.Sdk`
- Changed target from `netstandard2.0` to `net10.0`
- Replaced `EntityFramework 6` with `Microsoft.EntityFrameworkCore 10.0.0` + `Microsoft.EntityFrameworkCore.SqlServer 10.0.0`
- Removed: `Microsoft.CSharp`, `System.Data.DataSetExtensions` (built into SDK)
- Added `GenerateAssemblyInfo=false` to prevent duplicate attribute conflicts

**eShopLegacy.Utilities.csproj**
- Converted to SDK-style `Microsoft.NET.Sdk` targeting `net10.0`
- Removed: `Microsoft.AspNetCore.SystemWebAdapters` (no longer needed)
- Added: `Microsoft.AspNetCore.Http.Abstractions` for `IHttpContextAccessor`

**eShopLegacyMVC.csproj**
- Replaced verbose legacy .csproj with SDK-style `Microsoft.NET.Sdk.Web` targeting `net10.0`
- Removed all legacy packages: Autofac.Mvc5, Autofac.WebApi2, EntityFramework 6, log4net, Microsoft.Owin.*, Microsoft.AspNet.Identity.*, System.Web.Optimization, WebGrease, etc.
- Added: Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation 10.0.0, Microsoft.EntityFrameworkCore 10.0.0, Microsoft.EntityFrameworkCore.SqlServer 10.0.0, Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.0, Autofac 8.3.0, Autofac.Extensions.DependencyInjection 10.0.0, Newtonsoft.Json 13.0.3, Microsoft.AspNetCore.Mvc.NewtonsoftJson 10.0.0

### New Files Created

- **eShopLegacyMVC/Program.cs** — ASP.NET Core entry point replacing Global.asax + Startup.cs (OWIN)
- **eShopLegacyMVC/appsettings.json** — replaces Web.config connection strings and app settings
- **eShopLegacyMVC/appsettings.Development.json** — dev-specific overrides
- **eShopLegacyMVC/Views/_ViewImports.cshtml** — global Razor imports and tag helpers
- **eShopLegacyMVC/Views/Shared/_ValidationScriptsPartial.cshtml** — replaces @Scripts.Render bundles
- **eShopLegacyMVC/wwwroot/** — static file serving directory (Content, Scripts, Images, fonts, Pics)

### Source Code Migration

**System.Web removal**
- `CatalogBrand.cs`, `CatalogType.cs` — removed `using System.Web`
- `CatalogItem.cs` — removed `using System.Web`, added nullable annotations
- `PreconfiguredData.cs` — removed `using System.Web`
- `SessionDemoModel.cs` — made `StringSessionItem` nullable to eliminate CS8618 warning

**EF6 → EF Core**
- `CatalogDBContext.cs` — replaced `System.Data.Entity.DbContext` with `Microsoft.EntityFrameworkCore.DbContext`; constructor changed to accept `DbContextOptions<CatalogDBContext>`; fluent API updated (`EntityTypeConfiguration<T>` → `EntityTypeBuilder<T>`, `DatabaseGeneratedOption.None` → `ValueGeneratedNever()`, `HasRequired` → `HasOne`)
- `CatalogItemHiLoGenerator.cs` — replaced `db.Database.SqlQuery<Int64>` with `db.Database.SqlQueryRaw<long>`
- `CatalogService.cs` — replaced `System.Data.Entity` with `Microsoft.EntityFrameworkCore`
- `CatalogDBInitializer.cs` — replaced `CreateDatabaseIfNotExists<T>`, `System.Web.Hosting.HostingEnvironment`, `context.Database.ExecuteSqlCommand` with EF Core `ExecuteSqlRaw`; replaced `Database.SqlQuery` with `SqlQueryRaw`; replaced `ConfigurationManager.AppSettings` with constructor-injected `IHostEnvironment` + bool parameter; `AppDomain.CurrentDomain.BaseDirectory` replaced with `AppContext.BaseDirectory`

**ASP.NET Identity 2.x → ASP.NET Core Identity**
- `IdentityModels.cs` — replaced `Microsoft.AspNet.Identity.EntityFramework` with `Microsoft.AspNetCore.Identity.EntityFrameworkCore`; `ApplicationDbContext` constructor now accepts `DbContextOptions<ApplicationDbContext>`; `ApplicationUser.GenerateUserIdentityAsync` removed (not needed in ASP.NET Core Identity)
- `AccountController.cs` — replaced OWIN-based `HttpContext.GetOwinContext().Get<...>()` with constructor-injected `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>`; updated `SignInStatus` result handling; replaced `AuthenticationManager.SignOut` with `_signInManager.SignOutAsync()`; removed `ChallengeResult` inner class (not needed)

**Logging: log4net → Microsoft.Extensions.Logging**
- `CatalogController.cs`, `PicController.cs` — replaced `ILog` (log4net) with `ILogger<T>` (constructor injected); `_log.Info(...)` → `_log.LogInformation(...)`; `_log.Debug(...)` → `_log.LogDebug(...)`
- `Properties/AssemblyInfo.cs` — removed `log4net.Config.XmlConfigurator` attribute
- `Global.asax.cs` — replaced with stub (log4net references removed)

**OWIN/Startup → Program.cs**
- `Startup.cs` — replaced with stub; auth configured in Program.cs
- `App_Start/Startup.Auth.cs` — replaced with stub; cookie auth set up in Program.cs
- `App_Start/IdentityConfig.cs` — replaced with stub; `ApplicationUserManager`/`ApplicationSignInManager` replaced by built-in Identity services
- `App_Start/RouteConfig.cs` — replaced with stub; routing moved to Program.cs `MapControllerRoute`
- `App_Start/FilterConfig.cs` — replaced with stub; filters moved to Program.cs `AddControllersWithViews`
- `App_Start/WebApiConfig.cs` — replaced with stub; API routing moved to Program.cs `MapControllers`
- `App_Start/BundleConfig.cs` — replaced with stub; static files served directly from wwwroot

**Web API migration (System.Web.Http → Microsoft.AspNetCore.Mvc)**
- `Controllers/WebApi/BrandsController.cs` — replaced `ApiController` with `ControllerBase`, added `[ApiController]` + `[Route]` attributes, `IHttpActionResult` → `IActionResult`
- `Controllers/WebApi/FilesController.cs` — same migration; `HttpResponseMessage` with `StreamContent` replaced with `File(stream, ...)` result
- `Controllers/Api/CatalogController.cs` — replaced MVC `Controller` with `ControllerBase`, added `[ApiController]`

**MVC Controller migration**
- `CatalogController.cs` — replaced `System.Web.Mvc` with `Microsoft.AspNetCore.Mvc`; `HttpStatusCodeResult(HttpStatusCode.BadRequest)` → `BadRequest()`; `HttpNotFound()` → `NotFound()`; `Url.RouteUrl(name, values, scheme)` → same (API unchanged in ASP.NET Core); `SelectList` fully qualified
- `PicController.cs` — replaced `Server.MapPath("~/Pics")` with `IWebHostEnvironment.ContentRootPath` injection; added file existence check
- `UserInfoController.cs` — replaced `[Authorize]` from `System.Web.Mvc` with `Microsoft.AspNetCore.Authorization`
- `AspNetSessionController.cs` — replaced `HttpContext.Session["key"]` (legacy object storage) with `HttpContext.Session.GetString/SetString` + JSON serialization

**Utilities migration**
- `WebHelper.cs` — replaced `System.Web.HttpContext.Current.Request.Headers["User-Agent"]` with `IHttpContextAccessor`-based approach
- `Serializing.cs` — replaced `BinaryFormatter` (removed in .NET 9) with `System.Text.Json.JsonSerializer`

**Views migration**
- `Views/Shared/_Layout.cshtml` — replaced `@Styles.Render()`/`@Scripts.Render()` bundles with direct `<link>`/`<script>` tags; replaced `Html.Partial` with `Html.PartialAsync`; replaced `HttpContext.Current.Session["..."]` with `IHttpContextAccessor` injection; replaced `WebHelper.UserAgent` static call with injected `IHttpContextAccessor`
- `Views/Shared/_LoginPartial.cshtml` — replaced `@using Microsoft.AspNet.Identity` + `User.Identity.GetUserName()` with ASP.NET Core `User.Identity?.Name`; replaced HTML helper links with tag helpers
- `Views/Shared/Lockout.cshtml` — removed `@model System.Web.Mvc.HandleErrorInfo` (System.Web type)
- `Views/Catalog/Create.cshtml`, `Edit.cshtml` — replaced `@Scripts.Render("~/bundles/jqueryval")` with `@await Html.PartialAsync("_ValidationScriptsPartial")`
- `Views/Account/Login.cshtml` — replaced `Html.BeginForm(...)` with 5-arg overload (not in ASP.NET Core) with `<form asp-action...>` tag helper
- `Views/Account/Register.cshtml` — replaced `@Scripts.Render(...)` with `_ValidationScriptsPartial`
- `Views/AspNetSession/Index.cshtml` — replaced `@Scripts.Render(...)` with `_ValidationScriptsPartial`
- `Views/_ViewImports.cshtml` (new) — registers `Microsoft.AspNetCore.Mvc.TagHelpers`

## Behavioral Notes

- **BinaryFormatter removal**: `Serializing.SerializeBinary` now uses `System.Text.Json` instead of the removed `BinaryFormatter`. The wire format for `FilesController.Get()` changes from binary to JSON. Clients consuming this endpoint must update accordingly.
- **Session storage**: `AspNetSessionController` now uses JSON-serialized strings in session instead of raw object storage. Session data is not binary-compatible across the upgrade boundary.
- **Identity database schema**: The ASP.NET Core Identity schema adds `NormalizedUserName`, `NormalizedEmail`, `NormalizedName` columns and renames `LockoutEndDateUtc` → `LockoutEnd`. An existing identity database requires a schema migration before the upgraded app connects to it.
- **Pics path**: `PicController` now resolves `Pics/` relative to `ContentRootPath`. Ensure the `Pics` folder is deployed alongside the app (not under `wwwroot`).

## Next Steps

- Run `dotnet ef migrations add InitialCreate` for both `CatalogDBContext` and `ApplicationDbContext` to generate EF Core migration scripts.
- If upgrading an existing database, apply the Identity schema migration (new normalized columns, LockoutEnd rename).
- Static files (Content, Scripts, Images, fonts, Pics) have been copied to `wwwroot/`. Consider using a build-time bundler (WebOptimizer, Webpack) if bundle minification is needed in production.
- The `CatalogDBInitializer` is no longer auto-registered via `Database.SetInitializer`. Wire it up manually via `IHostedService` or startup code if database seeding is needed.
- The `ApplicationModule` (Autofac) no longer registers `CatalogDBContext` or `CatalogDBInitializer` — these are now managed by ASP.NET Core DI. Confirm this matches the intended lifetime management.
- Consider migrating `Serializing` callers (`FilesController`) to use a typed DTO endpoint rather than binary/JSON stream responses.
