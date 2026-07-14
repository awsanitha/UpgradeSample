using Microsoft.AspNetCore.Mvc;

namespace eShopLegacyMVC.Controllers
{
    public class UserInfoController : Controller
    {
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult Index()
        {
            return View();
        }
    }
}
