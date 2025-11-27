using ADV.Application.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ADV.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var products = new List<ProductDto>
            {
                new ProductDto { Id = 201, Title = "Smartphone Ultra X", Price = 899.99m, Category = "Mobile" },
                new ProductDto { Id = 202, Title = "Audífonos Bluetooth Pro", Price = 59.90m, Category = "Audio" },
                new ProductDto { Id = 203, Title = "Silla Gamer Ergonomic", Price = 199.50m, Category = "Furniture" },
                new ProductDto { Id = 204, Title = "Tablet Lite 10''", Price = 329.00m, Category = "Mobile" }
            };

            return Ok(products);
        }

    }
}
