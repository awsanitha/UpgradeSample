using eShopLegacyMVC.Services;
using log4net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace eShopLegacyMVC.Controllers
{
    public class PicController : Controller
    {
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        public const string GetPicRouteName = "GetPicRouteTemplate";

        private readonly ICatalogService service;
        private readonly IWebHostEnvironment _env;

        public PicController(ICatalogService service, IWebHostEnvironment env)
        {
            this.service = service;
            _env = env;
        }

        // GET: items/{catalogItemId}/pic
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
                var webRoot = _env.WebRootPath ?? _env.ContentRootPath;
                var path = Path.Combine(webRoot, "Pics", item.PictureFileName);

                if (!System.IO.File.Exists(path))
                {
                    return NotFound();
                }

                string imageFileExtension = Path.GetExtension(item.PictureFileName);
                string mimetype = GetImageMimeTypeFromImageFileExtension(imageFileExtension);

                var buffer = System.IO.File.ReadAllBytes(path);

                return File(buffer, mimetype);
            }

            return NotFound();
        }

        private string GetImageMimeTypeFromImageFileExtension(string extension)
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
                _ => "application/octet-stream",
            };
        }
    }
}
