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

        // Static accessor for use in views (requires IHttpContextAccessor registered in DI)
        private static IHttpContextAccessor? _staticAccessor;

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            _staticAccessor = httpContextAccessor;
        }

        public static string StaticUserAgent => _staticAccessor?.HttpContext?.Request.Headers["User-Agent"].ToString() ?? string.Empty;
    }
}
