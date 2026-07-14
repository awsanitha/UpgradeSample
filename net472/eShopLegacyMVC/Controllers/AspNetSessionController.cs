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
            var json = HttpContext.Session.GetString("DemoItem");
            if (!string.IsNullOrEmpty(json))
            {
                model = JsonSerializer.Deserialize<SessionDemoModel>(json);
            }
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
