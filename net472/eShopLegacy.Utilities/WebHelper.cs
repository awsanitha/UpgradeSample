using Microsoft.AspNetCore.Http;

namespace eShopLegacy.Utilities
{
    /// <summary>
    /// Provides HTTP context helper methods via IHttpContextAccessor.
    /// In ASP.NET Core, HttpContext.Current is not available; inject IHttpContextAccessor instead.
    /// </summary>
    public class WebHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public WebHelper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string UserAgent =>
            _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? string.Empty;
    }
}
