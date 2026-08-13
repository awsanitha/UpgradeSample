# Migration Summary: .NET Framework 4.7.2 → .NET 10

## Status: ✅ COMPLETE — Zero compilation errors, zero warnings

All three projects target `net10.0` and build cleanly:
- `eShopLegacy.Common` — Build succeeded (0 warnings, 0 errors)
- `eShopLegacy.Utilities` — Build succeeded (0 warnings, 0 errors)
- `eShopLegacyMVC` — Build succeeded (0 warnings, 0 errors)

---

## Changes Made

### eShopLegacy.Common
- **Target framework**: `netstandard2.0` → `net10.0`
- **Packages removed**: `EntityFramework 6.x`, `System.ComponentModel.Annotations`, `System.Data.DataSetExtensions`, `Microsoft.CSharp` (all built-in for net10.0)
- **Added** `GenerateAssemblyInfo=false` to suppress duplicate attribute errors from legacy `Properties/AssemblyInfo.cs`
- `Models/CatalogBrand.cs`, `CatalogType.cs` — removed unused `using System.Web`, `System.Linq`, `System.Collections.Generic`
- `Models/CatalogItem.cs` — updated non-nullable properties to use `= string.Empty` or `?` nullable annotations
- `Models/SessionDemoModel.cs` — updated `StringSessionItem` to `string?`
- `Utilities/Serializing.cs` — replaced removed `BinaryFormatter` with `System.Text.Json.JsonSerializer`; method signatures updated to `object?`

### eShopLegacy.Utilities
- **Target framework**: already `net10.0` (unchanged)
- **Packages removed**: `Microsoft.CSharp`, `System.Data.DataSetExtensions`, `Microsoft.AspNetCore.SystemWebAdapters` (no longer needed)
- **Package added**: `Microsoft.AspNetCore.Http.Abstractions 2.3.0` (for `IHttpContextAccessor`)
- `WebHelper.cs` — converted from `static` class using `System.Web.HttpContext.Current` to injectable class using `IHttpContextAccessor` (registered as `AddScoped` in Program.cs)

### eShopLegacyMVC
- **Project file** — complete rewrite from old-style `.csproj` format to SDK-style `Microsoft.NET.Sdk.Web` targeting `net10.0`
- **Packages removed**: All legacy Framework packages (Autofac.Mvc5, Autofac.WebApi2, Microsoft.Owin.*, Owin, System.Web.Http, System.Web.Mvc, EntityFramework 6.x, Microsoft.AspNet.Identity.*, log4net 2.x, WebGrease, System.Web.Optimization, etc.)
- **Packages added**:
  - `Autofac 8.3.0` + `Autofac.Extensions.DependencyInjection 10.0.0`
  - `log4net 3.3.2` (upgraded from 2.0.17 to fix NU1902 vulnerability)
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.0`
  - `Microsoft.EntityFrameworkCore.SqlServer 10.0.0`
  - `Microsoft.EntityFrameworkCore.Design 10.0.0`
  - `Newtonsoft.Json 13.0.3`
- **Legacy files excluded from compilation** (kept on disk for reference): `Global.asax.cs`, `Startup.cs`, `App_Start/Startup.Auth.cs`, `App_Start/BundleConfig.cs`, `App_Start/WebApiConfig.cs`, `App_Start/FilterConfig.cs`, `App_Start/RouteConfig.cs`, `App_Start/IdentityConfig.cs`, `Properties/AssemblyInfo.cs`

#### Program.cs (new — replaces Global.asax + Startup)
- Configured log4net, Autofac container factory, EF Core DbContexts, ASP.NET Core Identity, session, and HTTP context accessor
- Replaced `Database.SetInitializer` pattern with startup `CatalogDBInitializer.Initialize()`
- Replaced `Session_Start` with a session init middleware
- Replaced route registration with `MapControllerRoute`

#### Models
- `CatalogDBContext.cs` — migrated from EF6 `DbModelBuilder`/`EntityTypeConfiguration<T>` to EF Core `ModelBuilder`; parameterless constructor → `DbContextOptions<CatalogDBContext>` constructor; `DatabaseGeneratedOption.None` → `ValueGeneratedNever()`; `HasRequired().WithMany()` → `HasOne().WithMany().IsRequired()`
- `IdentityModels.cs` — `Microsoft.AspNet.Identity.EntityFramework` → `Microsoft.AspNetCore.Identity.EntityFrameworkCore`; `ApplicationDbContext` parameterless constructor → `DbContextOptions<ApplicationDbContext>`; removed `GenerateUserIdentityAsync` (not needed in ASP.NET Core Identity)
- `CatalogItemHiLoGenerator.cs` — replaced EF6 `Database.SqlQuery<Int64>` with ADO.NET `GetDbConnection().CreateCommand()`
- `Infrastructure/CatalogDBInitializer.cs` — removed EF6 `CreateDatabaseIfNotExists<T>` base class; converted to static `Initialize()` method using `Database.EnsureCreated()` and `ExecuteSqlRaw()`
- `Infrastructure/PreconfiguredData.cs` — removed unused `System.Web` using

#### Controllers
- All controllers migrated from `System.Web.Mvc`/`System.Web.Http` → `Microsoft.AspNetCore.Mvc`
- `AccountController.cs` — full rewrite: OWIN/identity 2.x → ASP.NET Core Identity via constructor injection; `SignInStatus` enum → `SignInResult`; removed `ChallengeResult`/`HttpUnauthorizedResult`; removed `IAuthenticationManager` OWIN dependency
- `CatalogController.cs` — `HttpStatusCodeResult(BadRequest)` → `BadRequest()`; `HttpNotFound()` → `NotFound()`; `Request.Url.Scheme` → `Request.Scheme`
- `PicController.cs` — `Server.MapPath("~/Pics")` → injected `IWebHostEnvironment.ContentRootPath`; `HttpStatusCodeResult` → `BadRequest()`/`NotFound()`; switch expression for mime types
- `BrandsController.cs` / `FilesController.cs` — `ApiController` → `ControllerBase`; `IHttpActionResult` → `IActionResult`; removed `System.Runtime.Remoting.Messaging`; `ResponseMessage(HttpResponseMessage)` → `Ok()`/`NotFound()`; added `[ApiController]`/`[Route]` attributes
- `AspNetSessionController.cs` — `HttpContext.Session["key"]` → `HttpContext.Session.GetString/SetString`; `SessionDemoModel` serialization via `System.Text.Json`; fixed namespace

#### Services
- `CatalogService.cs` — `System.Data.Entity` → `Microsoft.EntityFrameworkCore`; added `.ToList()` for deferred execution
- `CatalogServiceMock.cs` — removed unused System.Web imports

#### Views
- `Views/_ViewImports.cshtml` — **new file** replacing `Views/Web.config`; adds tag helpers and namespace imports
- `Views/Shared/_Layout.cshtml` — replaced `@Styles.Render`/`@Scripts.Render` with direct `<link>`/`<script>` tags; `HttpContext.Current.Session` → `Context.Session.GetString()`; `@WebHelper.UserAgent` → `@inject WebHelper`; `@Html.Partial` → `@await Html.PartialAsync`
- `Views/Shared/_LoginPartial.cshtml` — `@using Microsoft.AspNet.Identity` removed; `Request.IsAuthenticated` → `User.Identity?.IsAuthenticated`; `User.Identity.GetUserName()` → `User.Identity.Name`
- `Views/Shared/Lockout.cshtml` — removed `@model System.Web.Mvc.HandleErrorInfo`
- `Views/Account/Login.cshtml`, `Register.cshtml` — `@Scripts.Render` → direct `<script>` tags; `BeginForm` signature fix (added `null` for antiforgery parameter)
- `Views/Catalog/Create.cshtml`, `Edit.cshtml` — `@Scripts.Render` → direct `<script>` tags
- `Views/Catalog/Index.cshtml` — `@Html.Partial` → `@await Html.PartialAsync`
- `Views/AspNetSession/Index.cshtml` — `@Scripts.Render` → direct `<script>` tags

#### Configuration
- `appsettings.json` — **new file** containing connection strings and app settings migrated from `Web.config`
- `Web.config` and transform files excluded from SDK compilation

---

## Architecture Decisions

| Decision | Rationale |
|---|---|
| Kept Autofac | App has existing investment in `ApplicationModule`; `Autofac.Extensions.DependencyInjection` integrates cleanly with ASP.NET Core DI |
| Kept log4net (upgraded to 3.3.2) | Existing log4Net.xml config; upgrading eliminates NU1902 vulnerability advisory |
| BinaryFormatter → System.Text.Json | BinaryFormatter removed in .NET 5+; JSON serialization is the modern equivalent |
| EF6 initializer → static `Initialize()` | EF Core has no `IDatabaseInitializer`; explicit startup initialization is the standard pattern |
| `ApplicationDbContext` kept separate | Separation of Identity and catalog schemas is correct; each gets its own connection string |
| WebHelper → injectable class | `System.Web.HttpContext.Current` removed; `IHttpContextAccessor` is the ASP.NET Core equivalent |

---

## Next Steps (for subsequent cycles)

1. **Database migration**: Run `dotnet ef migrations add InitialCreate` for both `CatalogDBContext` and `ApplicationDbContext` to generate EF Core migration files before deploying to an existing database
2. **Identity schema backfill**: If migrating an existing ASP.NET Identity 2.x database, backfill the `NormalizedUserName`, `NormalizedEmail`, and `NormalizedName` columns, and rename `LockoutEndDateUtc` → `LockoutEnd` (type change to `datetimeoffset`)
3. **log4Net.xml compatibility**: Review `log4Net.xml` appender configuration — log4net 3.x may have minor API differences in some appender types
4. **wwwroot**: For production, consider moving static assets (`Content/`, `Scripts/`, `Images/`, `Pics/`, `fonts/`) to a `wwwroot/` folder as that is the default static file root in ASP.NET Core (currently using `ContentRootPath` for images)
5. **Session serialization**: The `SessionDemoModel` stored in session is now JSON-encoded; existing sessions will be invalidated after upgrade (expected behaviour for a major version migration)
6. **Integration tests**: Add integration tests using `WebApplicationFactory<Program>` to verify route mappings and authentication flows
7. **HTTPS redirection**: `UseHttpsRedirection()` is enabled; ensure SSL certificates are configured in deployment environments
