using eShopLegacy.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace eShopLegacyMVC.Controllers
{
    public class AspNetSessionController : Controller
    {
        // GET: AspNetSession
        public IActionResult Index()
        {
            var json = HttpContext.Session.GetString("DemoItem");
            SessionDemoModel? model = json != null
                ? JsonSerializer.Deserialize<SessionDemoModel>(json)
                : new SessionDemoModel();
            return View(model);
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
