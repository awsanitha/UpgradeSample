// Global.asax.cs is no longer used.
// Application startup is handled by Program.cs (ASP.NET Core minimal hosting).
// The following classes are kept for reference only and are now unused:

namespace eShopLegacyMVC
{
    // Previously: ActivityIdHelper - used for log4net correlation
    // Now: Use ILogger with structured logging via Microsoft.Extensions.Logging

    // Previously: WebRequestInfo - used for log4net request info
    // Now: IHttpContextAccessor provides access to HttpContext in DI-enabled services
}
