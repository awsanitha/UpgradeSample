using eShopLegacy.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace eShopLegacyMVCCore.Controllers
{
    public class AspNetSessionController : Controller
    {
        private const string DemoItemSessionKey = "DemoItem";

        // GET: AspNetSession
        public IActionResult Index()
        {
            SessionDemoModel? model = null;
            var sessionValue = HttpContext.Session.GetString(DemoItemSessionKey);
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
            HttpContext.Session.SetString(DemoItemSessionKey, JsonSerializer.Serialize(demoModel));
            return View(demoModel);
        }
    }
}
