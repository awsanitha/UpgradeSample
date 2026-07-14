# eShopLegacyMVC Migration Summary

## Migration: .NET Framework 4.7.2 → .NET 10.0

**Status: ✅ Build Succeeded — 0 Errors**

---

## Changes Made

### Project Files
- **eShopLegacyMVC.csproj**: Replaced legacy .NET Framework csproj with SDK-style `Microsoft.NET.Sdk.Web`, targeting `net10.0`
- **eShopLegacy.Common.csproj**: Updated to `net10.0`, replaced EF6 with `Microsoft.EntityFrameworkCore 9.0.0`
- **eShopLegacy.Utilities.csproj**: Updated to `net10.0`, removed `Microsoft.AspNetCore.SystemWebAdapters`
- **packages.config**: Removed (replaced by PackageReference in csproj)
- **Properties/AssemblyInfo.cs**: Removed from all projects (SDK auto-generates)

### Infrastructure / Startup
- **Program.cs** (new): Replaces `Global.asax.cs`, `Startup.cs`, and all `App_Start/` files
  - Configures Autofac via `UseServiceProviderFactory(new AutofacServiceProviderFactory())`
  - Registers EF Core for `CatalogDBContext` and `ApplicationDbContext` (Identity)
  - Configures ASP.NET Core Identity with cookie authentication
  - Registers session middleware (`AddSession` / `UseSession`)
  - Maps conventional MVC route and the named pic route
- **appsettings.json** (new): Replaces `Web.config` — connection strings and app settings
- **appsettings.Development.json** (new): Dev overrides with `UseMockData=true`
- **Views/_ViewImports.cshtml** (new): Adds `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers` and `@using Microsoft.AspNetCore.Http`

### Removed Legacy Files (excluded via `<Compile Remove>` in csproj)
- `Global.asax` / `Global.asax.cs`
- `Startup.cs` (OWIN startup)
- `App_Start/BundleConfig.cs`
- `App_Start/FilterConfig.cs`
- `App_Start/RouteConfig.cs`
- `App_Start/WebApiConfig.cs`
- `App_Start/Startup.Auth.cs`
- `App_Start/IdentityConfig.cs`

### EF6 → EF Core
- **CatalogDBContext**: Replaced `System.Data.Entity.DbContext` with `Microsoft.EntityFrameworkCore.DbContext`; converted `EntityTypeConfiguration<T>` to inline `EntityTypeBuilder<T>` calls; constructor now takes `DbContextOptions<CatalogDBContext>`
- **CatalogDBInitializer**: Removed `CreateDatabaseIfNotExists<T>` base class; replaced with a `Seed()` method called on startup; replaced `Database.SqlQuery<Int64>()` with `Database.SqlQueryRaw<long>()`; replaced `Database.ExecuteSqlCommand()` with `Database.ExecuteSqlRaw()`; `HostingEnvironment.ApplicationPhysicalPath` → `IWebHostEnvironment.ContentRootPath`; `ConfigurationManager.AppSettings` → `IConfiguration`
- **CatalogItemHiLoGenerator**: Replaced `db.Database.SqlQuery<Int64>()` with `db.Database.SqlQueryRaw<long>()`
- **CatalogService**: Updated to use EF Core (`Include`, `Entry`, `EntityState.Modified`)

### Authentication / Identity
- **IdentityModels.cs**: Replaced `Microsoft.AspNet.Identity.EntityFramework` with `Microsoft.AspNetCore.Identity.EntityFrameworkCore`; `ApplicationDbContext` constructor now takes `DbContextOptions<ApplicationDbContext>`
- **AccountController.cs**: Replaced OWIN `HttpContext.GetOwinContext()` pattern with constructor-injected `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>`; replaced `SignInStatus` enum with `SignInResult` booleans

### System.Web Removal
- **CatalogController**: `System.Web.Mvc.Controller` → `Microsoft.AspNetCore.Mvc.Controller`; `HttpStatusCodeResult(HttpStatusCode.BadRequest)` → `BadRequest()`; `HttpNotFound()` → `NotFound()`; `SelectList` from `Microsoft.AspNetCore.Mvc.Rendering`
- **PicController**: `Server.MapPath("~/Pics")` → `IWebHostEnvironment.WebRootPath`; injected `IWebHostEnvironment`
- **AspNetSessionController**: Fixed namespace; `HttpContext.Session["DemoItem"]` → `HttpContext.Session.GetString()` / `SetString()` with JSON serialization
- **UserInfoController**: `[Authorize]` from `Microsoft.AspNetCore.Authorization`
- **BrandsController**: Removed `System.Runtime.Remoting.Messaging`; `ApiController` → `ControllerBase` with `[ApiController]`; `IHttpActionResult` → `IActionResult`
- **FilesController**: Removed `System.Web.Http`; `ApiController` → `ControllerBase`; `HttpResponseMessage` → `IActionResult`

### Utilities
- **WebHelper.cs**: Replaced `HttpContext.Current.Request.Headers["User-Agent"]` with injected `IHttpContextAccessor`
- **Serializing.cs**: Replaced `BinaryFormatter` (removed in .NET 5+) with `System.Text.Json.JsonSerializer`

### Views
- **_Layout.cshtml**: Replaced `@Styles.Render()`/`@Scripts.Render()` with direct `<link>` and `<script>` tags; replaced `HttpContext.Current.Session["MachineName"]` with `Context.Session.GetString("MachineName")`; replaced `WebHelper.UserAgent` with `Context.Request.Headers["User-Agent"]`
- **_LoginPartial.cshtml**: Replaced `@using Microsoft.AspNet.Identity` / `Request.IsAuthenticated` / `User.Identity.GetUserName()` with ASP.NET Core equivalents
- **Lockout.cshtml**: Removed `@model System.Web.Mvc.HandleErrorInfo` (type doesn't exist in ASP.NET Core)
- **Create.cshtml, Edit.cshtml, Login.cshtml, Register.cshtml, AspNetSession/Index.cshtml**: Replaced `@Scripts.Render("~/bundles/jqueryval")` with direct `<script>` tags
- **Views/Web.config**: Excluded from build (Razor in ASP.NET Core doesn't use web.config)

### Static Files
- **wwwroot/**: Created with symlinks to `Content/`, `Scripts/`, `Pics/`, `fonts/`, `images/` directories for ASP.NET Core static file serving

---

## Next Steps (Non-Blocking)

1. **Session storage in _Layout.cshtml**: The layout displays `MachineName` and `SessionStartTime` from session. These are set by `Session_Start` in the legacy `Global.asax`. A middleware or filter should set these on first request (see note below).

2. **Log4net vulnerability**: `log4net 2.0.17` has a known moderate severity vulnerability (NU1902). Consider upgrading to `2.0.17` or a patched version when available.

3. **Nullable warnings (CS8618)**: Several models have non-nullable properties without initialization (CatalogItem, AccountViewModels, SessionDemoModel). Consider making properties nullable (`string?`) or adding required initialization.

4. **MVC1000 warnings**: `Html.Partial()` should be replaced with `<partial name="..." />` tag helper to avoid deadlocks in async contexts.

5. **EF1002 warning**: `SqlQueryRaw` with interpolated strings in `CatalogDBInitializer`. These use a static sequence name so SQL injection is not a concern here, but consider using `SqlQuery` when targeting EF Core 8+ for safety.

6. **Session MachineName/SessionStartTime**: The original `Global.asax` set these in `Session_Start`. To replicate, add a middleware or action filter that sets them on first request.

7. **Database migration strategy**: `Database.EnsureCreated()` is used for startup seeding. For production, consider using EF Core migrations (`dotnet ef migrations add Initial`).

8. **`Microsoft.AspNetCore.Session` package warning (NU1510)**: Session is included in `Microsoft.NET.Sdk.Web`; remove the explicit `Microsoft.AspNetCore.Session` PackageReference.

9. **Identity configuration**: Password requirements were copied from the legacy `IdentityConfig.cs`. Review and adjust for production security requirements.
