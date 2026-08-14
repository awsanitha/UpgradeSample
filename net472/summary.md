# .NET Framework → .NET 10 Migration Summary

## Status: ✅ BUILD SUCCEEDED — Zero Errors, Zero Warnings

All three projects build cleanly targeting net10.0 (libraries: netstandard2.0):

| Project | Target | Result |
|---------|--------|--------|
| eShopLegacy.Common | netstandard2.0 | ✅ 0 errors, 0 warnings |
| eShopLegacy.Utilities | net10.0 | ✅ 0 errors, 0 warnings |
| eShopLegacyMVC | net10.0 | ✅ 0 errors, 0 warnings |

---

## Changes Made

### eShopLegacy.Common
- Removed `EntityFramework 6.6.0` dependency (models are plain POCOs; EF is only needed in the MVC project).
- Removed `Microsoft.CSharp`, `System.Data.DataSetExtensions` (unused).
- Added `System.Text.Json 8.0.5` to support `Serializing.cs` on netstandard2.0.
- Fixed `CatalogBrand.cs` and `CatalogType.cs`: removed `using System.Web;` (no System.Web on netstandard2.0).
- Rewrote `Utilities/Serializing.cs`: replaced `BinaryFormatter` (removed in .NET 5+) with `System.Text.Json`. The serialization format changed from binary to JSON — callers consuming the raw stream (e.g. `FilesController`) will now receive JSON content.
- Added `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` to keep the existing `Properties/AssemblyInfo.cs`.

### eShopLegacy.Utilities
- Removed `Microsoft.AspNetCore.SystemWebAdapters` packages (System.Web compatibility shims are no longer needed).
- Removed `System.Data.DataSetExtensions`.
- Added `Microsoft.AspNetCore.Http.Abstractions` to access `HttpContext`.
- Rewrote `WebHelper.cs`: replaced static `HttpContext.Current` property with a static `UserAgent(HttpContext context)` method accepting the current `HttpContext`. Updated call sites in `_Layout.cshtml` to pass `Context`.

### eShopLegacyMVC (full migration)

#### Project File
- Replaced the legacy verbose MSBuild `.csproj` with a clean `Microsoft.NET.Sdk.Web` SDK-style project targeting `net10.0`.
- Excluded all legacy App_Start files, `Global.asax.cs`, OWIN `Startup.cs`, `packages.config`, `Web.config`, and `Properties/AssemblyInfo.cs` from compilation.
- Added modern packages: `Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.0`, `Microsoft.EntityFrameworkCore.SqlServer 10.0.0`, `Microsoft.EntityFrameworkCore.Tools 10.0.0`, `Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation 10.0.0`.

#### Program.cs (new — replaces Global.asax + OWIN Startup)
- Built-in DI replaces Autofac (`ApplicationModule` emptied).
- Session middleware with a `Session_Start`-equivalent `app.Use(...)` that sets `MachineName` and `SessionStartTime` on first access.
- EF Core `CatalogDBContext` and `ApplicationDbContext` registered via `AddDbContext`.
- ASP.NET Core Identity registered with `AddIdentity<ApplicationUser, IdentityRole>`.
- Cookie auth configured via `ConfigureApplicationCookie`.
- Static files served from `wwwroot` and also from the content root (preserving `Content/`, `Scripts/`, `Images/`, `Pics/`, `fonts/` at their original locations without moving them).
- `CatalogDBInitializer.Initialize()` called at startup when not using mock data.

#### Configuration
- `appsettings.json` created with connection strings and `AppSettings` from `Web.config`.
- `appsettings.Development.json` created for development overrides.

#### Data Access (EF6 → EF Core 10)
- `CatalogDBContext`: replaced `System.Data.Entity` / `DbModelBuilder` / `EntityTypeConfiguration<T>` with `Microsoft.EntityFrameworkCore` / `ModelBuilder` / `EntityTypeBuilder<T>`. `HasDatabaseGeneratedOption(None)` → `ValueGeneratedNever()`. `HasRequired<T>().WithMany()` → `HasOne<T>().WithMany()`.
- `CatalogItemHiLoGenerator`: `db.Database.SqlQuery<Int64>(...)` → EF Core 7+ `db.Database.SqlQuery<long>($"...")` with `using Microsoft.EntityFrameworkCore;`.
- `CatalogDBInitializer`: rewritten from `CreateDatabaseIfNotExists<T>` (EF6 initializer) to a plain service class; uses `context.Database.EnsureCreated()`, `ExecuteSqlRaw`, and `Database.SqlQuery<long>(...)`. Injected `IWebHostEnvironment` replaces `HostingEnvironment.ApplicationPhysicalPath`. Injected `IConfiguration` replaces `ConfigurationManager.AppSettings`.
- `CatalogService`: `System.Data.Entity` → `Microsoft.EntityFrameworkCore`; `EntityState.Modified` preserved.

#### Identity (ASP.NET Identity 2.x → ASP.NET Core Identity)
- `ApplicationUser` and `ApplicationDbContext` updated to inherit from `Microsoft.AspNetCore.Identity` / `Microsoft.AspNetCore.Identity.EntityFrameworkCore` types.
- `ApplicationDbContext` constructor now accepts `DbContextOptions<ApplicationDbContext>` for DI.
- `ApplicationUserManager` and `ApplicationSignInManager` (OWIN factories) removed; standard `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>` from ASP.NET Core Identity injected directly.
- `App_Start/IdentityConfig.cs` excluded from compilation.

#### Authentication (OWIN → ASP.NET Core)
- `Startup.Auth.cs` (OWIN `UseCookieAuthentication`) excluded; auth wired in `Program.cs` via `AddIdentity` + `ConfigureApplicationCookie`.
- `AccountController`: replaced OWIN `GetOwinContext()` with constructor-injected `UserManager`/`SignInManager`. `SignInStatus` → `SignInResult`. `AuthenticationManager.SignOut()` → `_signInManager.SignOutAsync()`. `ChallengeResult` inner class removed (not needed for current login/register flow).

#### Controllers
- All MVC controllers: `System.Web.Mvc.Controller` → `Microsoft.AspNetCore.Mvc.Controller`. `ActionResult` → `IActionResult`. `HttpStatusCodeResult(BadRequest)` → `BadRequest()`. `HttpNotFound()` → `NotFound()`.
- `PicController`: `Server.MapPath("~/Pics")` → `IWebHostEnvironment.ContentRootPath + "Pics"`.
- `AspNetSessionController`: `HttpContext.Session["DemoItem"]` (object) → `HttpContext.Session.GetString/SetString` with `System.Text.Json` serialization.
- `UserInfoController`: added `Microsoft.AspNetCore.Authorization` using.
- WebApi controllers: `ApiController` → `ControllerBase` with `[ApiController]`. `IHttpActionResult` → `IActionResult`. Route attributes preserved. `System.Runtime.Remoting.Messaging.CallContext` usage removed.

#### Bundling / Static Assets
- `BundleConfig.cs` excluded; `System.Web.Optimization` removed.
- `_Layout.cshtml` updated: `@Styles.Render(...)` / `@Scripts.Render(...)` replaced with explicit `<link>` and `<script>` tags. `HttpContext.Current.Session[...]` replaced with `Context.Session.GetString(...)`. `WebHelper.UserAgent` updated to `WebHelper.UserAgent(Context)`.
- `_LoginPartial.cshtml`: `@using Microsoft.AspNet.Identity` removed; `User.Identity.GetUserName()` → `User.Identity.Name`.
- `Views/Account/Login.cshtml`, `Register.cshtml`: `Scripts.Render` → direct `<script>` tags. `Html.BeginForm` 5-arg overload updated to include `antiforgery: null` parameter (ASP.NET Core signature).
- `Views/Catalog/Create.cshtml`, `Edit.cshtml`: `Scripts.Render` → direct `<script>` tags.
- `Views/Shared/Lockout.cshtml`: removed `@model System.Web.Mvc.HandleErrorInfo`.
- `Views/_ViewImports.cshtml` created with namespace usings and tag helpers.

---

## Next Steps

- **Database schema migration**: ASP.NET Core Identity schema differs from Identity 2.x. Before connecting to an existing database, run an EF Core migration and backfill the new `NormalizedUserName`, `NormalizedEmail`, `NormalizedName`, and `ConcurrencyStamp` columns, and rename `LockoutEndDateUtc` → `LockoutEnd` (type change to `datetimeoffset`).
- **Static file serving**: Static files (CSS, JS, Images, Pics, fonts) are currently served from the content root via `PhysicalFileProvider`. For production, consider moving them to a `wwwroot` folder (the conventional ASP.NET Core static-file location).
- **Serializing.cs format change**: `BinaryFormatter` (binary) was replaced with `System.Text.Json` (JSON). Any existing clients consuming the `FilesController` binary stream must be updated to parse JSON.
- **`FilesController` content type**: Updated to `application/json` to match the new JSON output.
- **Session object storage**: `AspNetSessionController` now JSON-serializes `SessionDemoModel` into the session string store. Existing sessions will be invalidated on upgrade.
- **EF Core migrations**: Run `dotnet ef migrations add Initial` and `dotnet ef database update` to create/update the catalog and identity databases.
- **Autofac removal**: `ApplicationModule.cs` is now an empty stub. All service registrations are in `Program.cs`. If Autofac is preferred over built-in DI, add `Autofac.Extensions.DependencyInjection` and restore module-based registration.
- **log4net removal**: Replaced by `Microsoft.Extensions.Logging` (built into ASP.NET Core). The `log4Net.xml` configuration file is no longer used. If specific log4net sinks (file appenders, etc.) are required, wire them via `builder.Logging.AddLog4Net()` with the `Microsoft.Extensions.Logging.Log4Net.AspNetCore` adapter.
