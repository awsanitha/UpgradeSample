using Microsoft.AspNetCore.Http;

namespace eShopLegacy.Utilities
{
    /// <summary>
    /// Web helper utilities. Migrated from System.Web.HttpContext to IHttpContextAccessor.
    /// </summary>
    public class WebHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public WebHelper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string UserAgent => _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? string.Empty;

        /// <summary>
        /// Static accessor for use in Razor views via DI.
        /// </summary>
        public static string GetUserAgent(IHttpContextAccessor httpContextAccessor)
            => httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? string.Empty;
    }
}
