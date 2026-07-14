using eShopLegacy.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace eShopLegacyMVC.Controllers
{
    public class AspNetSessionController : Controller
    {
        // GET: AspNetSession
        public IActionResult Index()
        {
            SessionDemoModel? model = null;
            var sessionData = HttpContext.Session.GetString("DemoItem");
            if (!string.IsNullOrEmpty(sessionData))
            {
                model = JsonConvert.DeserializeObject<SessionDemoModel>(sessionData);
            }
            return View(model);
        }

        // POST: AspNetSession
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(SessionDemoModel demoModel)
        {
            HttpContext.Session.SetString("DemoItem", JsonConvert.SerializeObject(demoModel));
            return View(demoModel);
        }
    }
}
