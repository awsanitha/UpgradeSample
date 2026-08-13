using eShopLegacy.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace eShopLegacyMVC.Controllers
{
    public class AspNetSessionController : Controller
    {
        // GET: AspNetSession
        public IActionResult Index()
        {
            SessionDemoModel? model = null;
            var sessionValue = HttpContext.Session.GetString("DemoItem");
            if (!string.IsNullOrEmpty(sessionValue))
            {
                model = JsonSerializer.Deserialize<SessionDemoModel>(sessionValue);
            }
            return View(model ?? new SessionDemoModel());
        }

        // POST: AspNetSession
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(SessionDemoModel demoModel)
        {
            HttpContext.Session.SetString("DemoItem", JsonSerializer.Serialize(demoModel));
            return View(demoModel);
        }
    }
}
