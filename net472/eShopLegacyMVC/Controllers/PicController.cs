using eShopLegacyMVC.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;

namespace eShopLegacyMVC.Controllers
{
    public class PicController : Controller
    {
        private readonly ILogger<PicController> _logger;
        private readonly ICatalogService _service;
        private readonly IWebHostEnvironment _env;

        public const string GetPicRouteName = "GetPicRouteTemplate";

        public PicController(ICatalogService service, IWebHostEnvironment env, ILogger<PicController> logger)
        {
            _service = service;
            _env = env;
            _logger = logger;
        }

        // GET: items/{catalogItemId}/pic
        [HttpGet]
        [Route("items/{catalogItemId:int}/pic", Name = GetPicRouteName)]
        public IActionResult Index(int catalogItemId)
        {
            _logger.LogInformation($"Now loading... /items/Index?{catalogItemId}/pic");

            if (catalogItemId <= 0)
                return BadRequest();

            var item = _service.FindCatalogItem(catalogItemId);
            if (item == null)
                return NotFound();

            var picsPath = Path.Combine(_env.ContentRootPath, "Pics");
            var path = Path.Combine(picsPath, item.PictureFileName ?? "dummy.png");

            if (!System.IO.File.Exists(path))
                return NotFound();

            var imageFileExtension = Path.GetExtension(item.PictureFileName);
            var mimetype = GetImageMimeTypeFromImageFileExtension(imageFileExtension);
            var buffer = System.IO.File.ReadAllBytes(path);

            return File(buffer, mimetype);
        }

        private static string GetImageMimeTypeFromImageFileExtension(string? extension)
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
