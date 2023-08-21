using eShopLegacyMVC.Services;
using System.Web;
using System.Web.Mvc;

namespace eShopLegacyMVC.Controllers
{
    public class DocumentsController : Controller
    {
        // GET: Files
        public ActionResult Index()
        {
            var files = FileService.Create().ListFiles();
            return View(files);
        }

        [OutputCache(VaryByParam = "filename", Duration = int.MaxValue)]
        public FileResult Download(string filename)
        {
            var fileService = FileService.Create();
            var file = fileService.DownloadFile(filename);
            FileContentResult fc = new FileContentResult(file, MimeMapping.GetMimeMapping(filename));
            fc.FileDownloadName = filename;
            return fc;
        }

        public ActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadDocument()
        {
            var fileService = FileService.Create();
            fileService.UploadFile(Request.Files);
            return RedirectToAction("Index");
        }
    }
}