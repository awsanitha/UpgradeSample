using eShopLegacy.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace eShopLegacyMVCCore.Controllers
{
    public class AspNetSessionController : Controller
    {
        // GET: AspNetCoreSession
        public IActionResult Index()
        {
            SessionDemoModel? model = null;
            var sessionValue = HttpContext.Session.GetString("DemoItem");
            if (sessionValue != null)
            {
                model = JsonSerializer.Deserialize<SessionDemoModel>(sessionValue);
            }
            return View(model);
        }

        // POST: AspNetCoreSession
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(SessionDemoModel demoModel)
        {
            HttpContext.Session.SetString("DemoItem", JsonSerializer.Serialize(demoModel));
            return View(demoModel);
        }
    }
}
