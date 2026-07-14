using eShopLegacyMVC.Models;
using eShopLegacyMVC.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace eShopLegacyMVC.Controllers.WebApi
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandsController : ControllerBase
    {
        private readonly ICatalogService _service;

        public BrandsController(ICatalogService service)
        {
            _service = service;
        }

        // GET api/brands
        [HttpGet]
        public ActionResult<IEnumerable<CatalogBrand>> Get()
        {
            var brands = _service.GetCatalogBrands();
            return Ok(brands);
        }

        // GET api/brands/5
        [HttpGet("{id}")]
        public ActionResult<CatalogBrand> Get(int id)
        {
            var brands = _service.GetCatalogBrands();
            var brand = brands.FirstOrDefault(x => x.Id == id);
            if (brand == null) return NotFound();

            return Ok(brand);
        }

        // DELETE api/brands/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var brandToDelete = _service.GetCatalogBrands().FirstOrDefault(x => x.Id == id);
            if (brandToDelete == null)
            {
                return NotFound();
            }

            // demo only - don't actually delete
            return Ok();
        }
    }
}
