using eShopLegacyMVC.Services;
using log4net;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

namespace eShopLegacyMVC.Controllers
{
    public class PicController : Controller
    {
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        public const string GetPicRouteName = "GetPicRouteTemplate";

        private readonly ICatalogService service;
        private readonly IWebHostEnvironment env;

        public PicController(ICatalogService service, IWebHostEnvironment env)
        {
            this.service = service;
            this.env = env;
        }

        // GET: Pic/5.png
        [HttpGet]
        [Route("items/{catalogItemId:int}/pic", Name = GetPicRouteName)]
        public IActionResult Index(int catalogItemId)
        {
            _log.Info($"Now loading... /items/Index?{catalogItemId}/pic");

            if (catalogItemId <= 0)
            {
                return BadRequest();
            }

            var item = service.FindCatalogItem(catalogItemId);

            if (item != null)
            {
                var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
                var picsPath = Path.Combine(webRoot, "Pics");

                // Fallback: try ContentRootPath/Pics if wwwroot/Pics doesn't exist
                if (!Directory.Exists(picsPath))
                {
                    picsPath = Path.Combine(env.ContentRootPath, "Pics");
                }

                var path = Path.Combine(picsPath, item.PictureFileName ?? "dummy.png");

                string imageFileExtension = Path.GetExtension(item.PictureFileName ?? string.Empty);
                string mimetype = GetImageMimeTypeFromImageFileExtension(imageFileExtension);

                if (System.IO.File.Exists(path))
                {
                    var buffer = System.IO.File.ReadAllBytes(path);
                    return File(buffer, mimetype);
                }
            }

            return NotFound();
        }

        private static string GetImageMimeTypeFromImageFileExtension(string extension)
        {
            return extension switch
            {
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".bmp" => "image/bmp",
                ".tiff" => "image/tiff",
                ".wmf" => "image/wmf",
                ".jp2" => "image/jp2",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };
        }
    }
}
