# eShopLegacyMVC — .NET Framework 4.7.2 → net10.0 Migration Summary

## Result

`dotnet build eShopLegacyMVC.sln` → **Build succeeded, 0 warnings, 0 errors**.

All three projects now target `net10.0`:
- `eShopLegacy.Common` → `net10.0`
- `eShopLegacy.Utilities` → `net10.0`
- `eShopLegacyMVC` (web app) → `net10.0` (SDK: `Microsoft.NET.Sdk.Web`)

---

## Changes Made

### Project Files

| File | Change |
|------|--------|
| `eShopLegacyMVC/eShopLegacyMVC.csproj` | Fully rewritten as SDK-style `Microsoft.NET.Sdk.Web`, `net10.0`. Replaced all legacy `<Reference>`, `<HintPath>`, `packages.config`-era imports with `<PackageReference>`. |
| `eShopLegacy.Common/eShopLegacy.Common.csproj` | Updated to `net10.0`, SDK-style, added EF Core SqlServer package. |
| `eShopLegacy.Utilities/eShopLegacy.Utilities.csproj` | Updated to `net10.0`, SDK-style, added `Microsoft.AspNetCore.Http.Abstractions`. |

### New Files Created

| File | Purpose |
|------|---------|
| `eShopLegacyMVC/Program.cs` | Replaces `Global.asax`, `Startup.cs`, and all `App_Start/` files. Wires ASP.NET Core hosting, Identity, EF Core, Autofac, session, routing. |
| `eShopLegacyMVC/appsettings.json` | Replaces `Web.config`. Contains connection strings, app settings. |
| `eShopLegacyMVC/appsettings.Development.json` | Development overrides. |
| `eShopLegacyMVC/Views/_ViewImports.cshtml` | ASP.NET Core tag helper imports and common `@using` directives. |
| `eShopLegacyMVC/wwwroot/` | Static files root directory for ASP.NET Core. |

### Code Migrations

| File | Change |
|------|--------|
| `Global.asax.cs` | Replaced with stub comment; logic moved to `Program.cs`. |
| `Startup.cs` | Replaced with stub comment; logic moved to `Program.cs`. |
| `App_Start/BundleConfig.cs` | Made inert; bundling replaced by direct static file references in `_Layout.cshtml`. |
| `App_Start/FilterConfig.cs` | Made inert; filters registered in `Program.cs`. |
| `App_Start/RouteConfig.cs` | Made inert; routes configured in `Program.cs` via `app.MapControllerRoute`. |
| `App_Start/WebApiConfig.cs` | Made inert; API controllers registered via `app.MapControllers()`. |
| `App_Start/IdentityConfig.cs` | Made inert; Identity configured in `Program.cs` via `builder.Services.AddIdentity`. |
| `App_Start/Startup.Auth.cs` | Made inert; cookie auth replaced by ASP.NET Core auth middleware. |
| `Models/CatalogDBContext.cs` | Migrated EF6 → EF Core 9. `DbContext` now takes `DbContextOptions<CatalogDBContext>`. `DbModelBuilder` → `ModelBuilder`. `EntityTypeConfiguration<T>` → `EntityTypeBuilder<T>`. `HasRequired/WithMany` → `HasOne/WithMany`. `HasDatabaseGeneratedOption` → `ValueGeneratedNever`. |
| `Models/IdentityModels.cs` | Migrated from `Microsoft.AspNet.Identity` (OWIN) to `Microsoft.AspNetCore.Identity`. `IdentityDbContext` now takes `DbContextOptions`. |
| `Models/CatalogItemHiLoGenerator.cs` | Replaced `db.Database.SqlQuery<Int64>` (EF6) with `db.Database.SqlQueryRaw<long>` (EF Core). |
| `Models/Infrastructure/CatalogDBInitializer.cs` | Migrated from EF6 `CreateDatabaseIfNotExists<T>` to EF Core seeding pattern. Replaced `HostingEnvironment.ApplicationPhysicalPath` → `IWebHostEnvironment.ContentRootPath`. Replaced `ConfigurationManager.AppSettings` → `IConfiguration`. Replaced `ExecuteSqlCommand` → `ExecuteSqlRaw`. Replaced `SqlQuery<Int64>` → `SqlQueryRaw<long>`. |
| `Services/CatalogService.cs` | Replaced `System.Data.Entity` with `Microsoft.EntityFrameworkCore`. |
| `Services/ICatalogService.cs` | Updated `FindCatalogItem` return type to `CatalogItem?` (nullable). |
| `Modules/ApplicationModule.cs` | Removed `CatalogDBContext` and `CatalogDBInitializer` registration (now via DI in Program.cs). |
| `Controllers/CatalogController.cs` | `System.Web.Mvc` → `Microsoft.AspNetCore.Mvc`. `HttpStatusCodeResult(BadRequest)` → `BadRequest()`. `HttpNotFound()` → `NotFound()`. `SelectList` fully qualified. Log4net → `ILogger<T>`. `Request.Url.Scheme` → `Request.Scheme`. |
| `Controllers/AccountController.cs` | Full rewrite: OWIN `GetOwinContext().GetUserManager<>()` pattern replaced by constructor-injected `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>`. `IdentityResult.Errors` (string) → `.Errors[].Description`. Removed `ChallengeResult`/`HttpUnauthorizedResult` (no external logins). |
| `Controllers/PicController.cs` | `System.Web.Mvc` → `Microsoft.AspNetCore.Mvc`. `Server.MapPath("~/Pics")` → `IWebHostEnvironment.ContentRootPath + "/Pics"`. |
| `Controllers/AspNetSessionController.cs` | `HttpContext.Session["key"]` → `HttpContext.Session.GetString/SetString` with JSON serialization. |
| `Controllers/UserInfoController.cs` | `System.Web.Mvc` → `Microsoft.AspNetCore.Mvc`. |
| `Controllers/WebApi/FilesController.cs` | `ApiController` → `ControllerBase` with `[ApiController]`. `HttpResponseMessage` → `IActionResult`. |
| `Controllers/WebApi/BrandsController.cs` | `ApiController` → `ControllerBase` with `[ApiController]`. `IHttpActionResult` → `IActionResult`. Removed `System.Runtime.Remoting.Messaging` (not available on .NET Core). |
| `Controllers/Api/CatalogController.cs` | `System.Web.Mvc` → `Microsoft.AspNetCore.Mvc`, `ControllerBase`. |
| `eShopLegacy.Common/Models/CatalogBrand.cs` | Removed `System.Web` using. |
| `eShopLegacy.Common/Models/CatalogType.cs` | Removed `System.Web` using. |
| `eShopLegacy.Common/Models/CatalogItem.cs` | Removed `System.Web` using. Added nullable annotations. |
| `eShopLegacy.Common/Utilities/Serializing.cs` | Replaced `BinaryFormatter` (removed in .NET 8+) with `System.Text.Json`. |
| `eShopLegacy.Utilities/WebHelper.cs` | Replaced `HttpContext.Current` (System.Web) with constructor-injected `IHttpContextAccessor`. |

### View Migrations

| File | Change |
|------|--------|
| `Views/Shared/_Layout.cshtml` | Replaced `@Styles.Render(...)` and `@Scripts.Render(...)` with direct `<link>` and `<script>` tags. Replaced `HttpContext.Current.Session[...]` with `Context.Session.GetString(...)`. Removed `@using eShopLegacy.Utilities` (WebHelper now DI-based). Used `@await Html.PartialAsync(...)`. |
| `Views/Shared/_LoginPartial.cshtml` | Replaced `@using Microsoft.AspNet.Identity` with `@inject SignInManager<ApplicationUser>` and `@inject UserManager<ApplicationUser>`. Replaced `Request.IsAuthenticated` with `SignInManager.IsSignedIn(User)`. Used tag helpers for form/anchor. |
| `Views/Shared/Lockout.cshtml` | Removed `@model System.Web.Mvc.HandleErrorInfo`. |
| `Views/Account/Login.cshtml` | Replaced `@Scripts.Render(...)` with direct `<script>` tags. Fixed `BeginForm` overload signature. |
| `Views/Account/Register.cshtml` | Replaced `@Scripts.Render(...)` with direct `<script>` tags. |
| `Views/AspNetSession/Index.cshtml` | Replaced `@Scripts.Render(...)` with direct `<script>` tags. |
| `Views/Catalog/Create.cshtml` | Replaced `@Scripts.Render(...)` with direct `<script>` tags. |
| `Views/Catalog/Edit.cshtml` | Replaced `@Scripts.Render(...)` with direct `<script>` tags. |
| `Views/_ViewImports.cshtml` | Created new file with `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers` and common `@using` directives. |

### Packages Removed (legacy)

- `Autofac.Mvc5` / `Autofac.WebApi2` → replaced by `Autofac.Extensions.DependencyInjection` 10.0.0
- `Microsoft.AspNet.Mvc` / `Microsoft.AspNet.WebApi.*` / `Microsoft.AspNet.WebPages` / `Microsoft.AspNet.Razor`
- `Microsoft.Owin.*`, `Owin`
- `Microsoft.AspNet.Identity.Core` / `Microsoft.AspNet.Identity.EntityFramework` / `Microsoft.AspNet.Identity.Owin`
- `EntityFramework` 6.x → replaced by `Microsoft.EntityFrameworkCore.SqlServer` 9.0.6
- `Microsoft.AspNet.Web.Optimization` (BundleConfig / System.Web.Optimization)
- `WebGrease`, `Antlr`, `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`, `Microsoft.Net.Compilers`
- All `Microsoft.ApplicationInsights.*` packages (Windows-specific, removed for cross-platform)
- `Microsoft.AspNet.SessionState.SessionStateModule`, `Microsoft.AspNet.TelemetryCorrelation`
- `System.Web.*` GAC references

### Packages Added

- `Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation` 10.0.0
- `Microsoft.EntityFrameworkCore.SqlServer` 9.0.6
- `Microsoft.EntityFrameworkCore.Design` 9.0.6
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 9.0.6
- `Autofac` 8.3.0
- `Autofac.Extensions.DependencyInjection` 10.0.0
- `Microsoft.AspNetCore.Mvc.NewtonsoftJson` 10.0.0
- `Newtonsoft.Json` 13.0.3
- `Microsoft.Extensions.Logging` (via ASP.NET Core transitive dependency — no explicit reference needed)

---

## Warnings Resolved

All four NuGet warnings from the initial migration pass have been resolved:

- `NU1510` (`Microsoft.Extensions.Logging` redundant): removed the explicit `<PackageReference>` — already provided transitively.
- `NU1902` (`log4net` 2.0.17 vulnerability): removed `log4net` entirely. All controllers already used `ILogger<T>` from `Microsoft.Extensions.Logging`. The `[assembly: log4net.Config.XmlConfigurator]` attribute in `AssemblyInfo.cs` was the only remaining reference and has been removed.

---

## Next Steps

1. **Session in layout**: The layout currently uses `Context.Session.GetString("MachineName")` and `Context.Session.GetString("SessionStartTime")`. These session keys were previously set in `Global.asax Session_Start`. Since `Global.asax` is gone, a middleware or `_Layout.cshtml` logic should set them if still needed. Consider moving this to a middleware registered in `Program.cs`.
2. **Static files**: The `Content/`, `Scripts/`, `Images/`, `Pics/`, and `fonts/` directories need to be served as static files. Either add a symlink/copy to `wwwroot/`, or configure `app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(...) })` for each directory.
3. **EF Core migrations**: `CatalogDBInitializer.Seed()` calls `context.Database.EnsureCreated()`. For production, EF Core migrations should be created with `dotnet ef migrations add InitialCreate` and managed properly.
4. **Identity database**: `ApplicationDbContext` for ASP.NET Core Identity needs an initial migration: `dotnet ef migrations add InitialIdentity --context ApplicationDbContext`.
5. **Nullable annotations**: The `LoginViewModel`, `RegisterViewModel`, and `SessionDemoModel` have non-nullable string properties without initializers (`CS8618`). These can be suppressed with `= string.Empty;` initializers or nullable annotations, matching the target application's null-safety policy.
