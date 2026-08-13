using Microsoft.AspNetCore.Mvc;

namespace eShopLegacyMVC.Controllers.Api
{
    [ApiController]
    [Route("api")]
    public class CatalogController2 : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok(new { Message = "Hello World!" });
        }
    }
}
