using Microsoft.AspNetCore.Http;

namespace eShopLegacy.Utilities
{
    public class WebHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public WebHelper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string UserAgent => _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? string.Empty;

        /// <summary>
        /// Static accessor for use in Razor views via DI - use constructor injection wherever possible.
        /// </summary>
        public static string GetUserAgent(IHttpContextAccessor accessor)
        {
            return accessor?.HttpContext?.Request.Headers["User-Agent"].ToString() ?? string.Empty;
        }
    }
}
