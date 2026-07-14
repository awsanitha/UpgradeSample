# eShopLegacyMVC — .NET Framework 4.7.2 → .NET 10 Migration Summary

## Result

`dotnet build eShopLegacyMVC.sln --configuration Release` → **Build succeeded, 0 errors.**

All three projects now target `net10.0`.

---

## Changes Made

### Project Files

| File | Change |
|------|--------|
| `eShopLegacyMVC/eShopLegacyMVC.csproj` | Converted from legacy XML-style to SDK-style (`Microsoft.NET.Sdk.Web`), targeting `net10.0`. Replaced ~30 `<Reference>` entries with modern `<PackageReference>` items. |
| `eShopLegacy.Common/eShopLegacy.Common.csproj` | Converted to SDK-style, targeting `net10.0`. Replaced EF6 with EF Core 9. |
| `eShopLegacy.Utilities/eShopLegacy.Utilities.csproj` | Converted to SDK-style, targeting `net10.0`. Added `Microsoft.AspNetCore.Http.Abstractions`. |

### Startup / Hosting

| File | Change |
|------|--------|
| `Program.cs` *(new)* | Replaces `Global.asax`, `Global.asax.cs`, `Startup.cs`, and all `App_Start/*.cs` files. Configures Autofac as the DI container, EF Core DbContexts, ASP.NET Core Identity, session, static files, routing, and cookie auth. |
| `appsettings.json` *(new)* | Migrated settings from `Web.config` (`connectionStrings`, `appSettings`). |
| `appsettings.Development.json` *(new)* | Development overrides. |
| Deleted: `Global.asax`, `Global.asax.cs`, `Startup.cs`, `App_Start/BundleConfig.cs`, `App_Start/FilterConfig.cs`, `App_Start/RouteConfig.cs`, `App_Start/WebApiConfig.cs`, `App_Start/IdentityConfig.cs`, `App_Start/Startup.Auth.cs`, `Web.config`, `Web.Debug.config`, `Web.Release.config`, `packages.config`, `ApplicationInsights.config` | All legacy ASP.NET Framework startup/config artifacts removed. |

### Data Access (EF6 → EF Core 9)

| File | Change |
|------|--------|
| `Models/CatalogDBContext.cs` | Migrated from `System.Data.Entity.DbContext` (EF6) to `Microsoft.EntityFrameworkCore.DbContext`. Updated `OnModelCreating` to use `ModelBuilder` + `EntityTypeBuilder<T>`. Constructor now takes `DbContextOptions<CatalogDBContext>`. |
| `Models/CatalogItemHiLoGenerator.cs` | Replaced `db.Database.SqlQuery<Int64>` (EF6) with `db.Database.SqlQueryRaw<long>` (EF Core). |
| `Models/Infrastructure/CatalogDBInitializer.cs` | Migrated from `CreateDatabaseIfNotExists<CatalogDBContext>` (EF6) to a POCO seeder class using `context.Database.EnsureCreated()`. Replaced `ExecuteSqlCommand` with `ExecuteSqlRaw`, and `SqlQuery` with `SqlQueryRaw`. Replaced `HostingEnvironment.ApplicationPhysicalPath` with injected `contentRootPath`. |
| `Services/CatalogService.cs` | Replaced `System.Data.Entity` namespace. Removed EF6-specific `EntityState` import (EF Core provides it via `Microsoft.EntityFrameworkCore`). |

### Identity (ASP.NET Identity 2 + OWIN → ASP.NET Core Identity 9)

| File | Change |
|------|--------|
| `Models/IdentityModels.cs` | Replaced `IdentityDbContext<ApplicationUser>` (OWIN) with `Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<ApplicationUser>`. Constructor takes `DbContextOptions<ApplicationDbContext>`. |
| `App_Start/IdentityConfig.cs` | Deleted. `ApplicationUserManager` and `ApplicationSignInManager` replaced by the built-in ASP.NET Core Identity services registered in `Program.cs`. |
| `App_Start/Startup.Auth.cs` | Deleted. Cookie auth configured in `Program.cs` via `ConfigureApplicationCookie`. |
| `Controllers/AccountController.cs` | Replaced OWIN-based `HttpContext.GetOwinContext()` lookups with constructor-injected `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>`. Updated `IdentityResult.Errors` to use `.Description` property (ASP.NET Core). |

### Controllers

| File | Change |
|------|--------|
| `Controllers/CatalogController.cs` | `System.Web.Mvc` → `Microsoft.AspNetCore.Mvc`. `ActionResult` → `IActionResult`. `HttpStatusCodeResult(HttpStatusCode.BadRequest)` → `BadRequest()`. `HttpNotFound()` → `NotFound()`. `this.Request.Url.Scheme` → `Request.Scheme`. |
| `Controllers/PicController.cs` | Replaced `Server.MapPath("~/Pics")` with `IWebHostEnvironment.ContentRootPath`. |
| `Controllers/AspNetSessionController.cs` | Replaced `HttpContext.Session["key"]` (object-based) with `HttpContext.Session.GetString/SetString` + JSON serialization. Added `using Microsoft.AspNetCore.Http`. Fixed namespace from `eShopLegacyMVCCore` to `eShopLegacyMVC`. |
| `Controllers/UserInfoController.cs` | `System.Web.Mvc` → `Microsoft.AspNetCore.Mvc`. |
| `Controllers/WebApi/BrandsController.cs` | Replaced `ApiController` (WebApi 2) + `IHttpActionResult` with `ControllerBase` + `IActionResult`/`ActionResult<T>`. Removed `System.Runtime.Remoting.Messaging` (removed in .NET Core). |
| `Controllers/WebApi/FilesController.cs` | Same as above. Replaced `HttpResponseMessage` with `IActionResult`. |
| `Controllers/Api/CatalogController.cs` | Replaced `System.Web.Mvc.Controller` with `ControllerBase`, `[ApiController]`. |

### DI / Autofac

| File | Change |
|------|--------|
| `Modules/ApplicationModule.cs` | Removed `Autofac.Mvc5`/`Autofac.WebApi2` registrations. Simplified to register `ICatalogService` only. `CatalogItemHiLoGenerator` now registered directly in .NET DI as singleton. |

### Utilities

| File | Change |
|------|--------|
| `eShopLegacy.Utilities/WebHelper.cs` | Replaced `System.Web.HttpContext.Current.Request` with `IHttpContextAccessor`. Added static accessor pattern for view usage. |
| `eShopLegacy.Common/Utilities/Serializing.cs` | Replaced `BinaryFormatter` (removed in .NET 9) with `System.Text.Json.JsonSerializer`. |

### Views

| File | Change |
|------|--------|
| `Views/_ViewImports.cshtml` *(new)* | Added standard ASP.NET Core view imports and `@addTagHelper`. |
| `Views/Shared/_Layout.cshtml` | Removed `@Styles.Render`/`@Scripts.Render` (bundling removed). Replaced with direct `<link>` and `<script>` tags. Replaced `HttpContext.Current.Session` with `IHttpContextAccessor` injection + `Session.GetString()`. Updated `Html.Partial` → `Html.PartialAsync`. |
| `Views/Shared/_LoginPartial.cshtml` | Replaced `Microsoft.AspNet.Identity` + `Request.IsAuthenticated` with `@inject SignInManager<ApplicationUser>` + `SignInManager.IsSignedIn(User)`. Used tag helpers for form/links. |
| `Views/Shared/Lockout.cshtml` | Removed `@model System.Web.Mvc.HandleErrorInfo` (type doesn't exist in ASP.NET Core). |
| `Views/Account/Login.cshtml` | Replaced `Html.BeginForm` overload (signature changed in ASP.NET Core) with `<form asp-action ...>` tag helper. |
| All views with `@Scripts.Render` | Replaced with direct `<script>` tags in `@section scripts {}` blocks. |
| `Views/Shared/Error.cshtml` | No change needed (already plain HTML). |

### Models

| File | Change |
|------|--------|
| `eShopLegacy.Common/Models/CatalogBrand.cs` | Removed `System.Web` usings. Added nullable annotations. |
| `eShopLegacy.Common/Models/CatalogType.cs` | Same. |
| `eShopLegacy.Common/Models/CatalogItem.cs` | Removed `System.Web` dependency. Added nullable annotations (`?` on navigation properties and optional strings). |
| `Models/Infrastructure/PreconfiguredData.cs` | Removed `System.Web` usings. |

---

## Packages Added (eShopLegacyMVC)

| Package | Version | Replaces |
|---------|---------|---------|
| `Autofac` | 8.1.0 | Autofac 4.9.1 |
| `Autofac.Extensions.DependencyInjection` | 10.0.0 | Autofac.Mvc5, Autofac.WebApi2 |
| `Microsoft.EntityFrameworkCore` | 9.0.5 | EntityFramework 6.2.0 |
| `Microsoft.EntityFrameworkCore.SqlServer` | 9.0.5 | EntityFramework.SqlServer 6.2.0 |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 9.0.5 | Microsoft.AspNet.Identity.* |
| `log4net` | 2.0.17 | log4net 2.0.8 |
| `Newtonsoft.Json` | 13.0.3 | Newtonsoft.Json 12.0.1 |
| `Microsoft.AspNetCore.Mvc.NewtonsoftJson` | 9.0.5 | *(new)* |

## Packages Removed

- `Autofac.Mvc5`, `Autofac.WebApi2`
- `EntityFramework` 6.x (EF6)
- `Microsoft.AspNet.Identity.*`
- `Microsoft.AspNet.Mvc`, `Microsoft.AspNet.WebApi.*`, `Microsoft.AspNet.WebPages.*`
- `Microsoft.Owin.*`, `Owin`
- `Microsoft.AspNet.Web.Optimization` (bundling)
- `WebGrease`, `Antlr3.Runtime`
- `Microsoft.ApplicationInsights.*` (Application Insights for .NET Framework)
- `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`
- `Microsoft.Web.Infrastructure`
- Various `System.*` polyfill packages

---

## Next Steps

- **log4net vulnerability**: `log4net 2.0.17` has a known moderate severity vulnerability (GHSA-4f7c-pmjv-c25w). Consider upgrading to `2.0.18+` when available, or replacing with `Microsoft.Extensions.Logging` + Serilog.
- **Static files**: The app serves static files (`Content/`, `Scripts/`, `Images/`, `Pics/`, `fonts/`) from the project content root. For production deployment, consider moving these into a `wwwroot/` folder for conventional ASP.NET Core layout.
- **Database seeding**: The `CatalogDBInitializer` now seeds only when `UseMockData=false` and the database is empty. Ensure SQL Server connection strings are configured for non-mock environments.
- **Application Insights**: The legacy `ApplicationInsights.config` was removed. If telemetry is needed, add `Microsoft.ApplicationInsights.AspNetCore` package and configure via `builder.Services.AddApplicationInsightsTelemetry()`.
- **Session machinename**: The session items `MachineName` and `SessionStartTime` previously set in `Session_Start` (Global.asax) are no longer automatically populated. Add middleware or filter to set them if required.
- **HTTPS redirect**: Currently enabled unconditionally. Review for local development environments.
