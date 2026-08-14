using Microsoft.AspNetCore.Http;

namespace eShopLegacy.Utilities
{
    public static class WebHelper
    {
        public static string UserAgent(HttpContext context)
            => context.Request.Headers["User-Agent"].ToString();
    }
}
