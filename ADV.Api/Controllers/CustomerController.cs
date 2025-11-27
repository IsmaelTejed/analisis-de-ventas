using ADV.Application.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ADV.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var customers = new List<CustomerDto>
            {
       
                new CustomerDto { Id = 501, Name = "Juan Pérez", Email = "juan@example.com", Country = "España" },
                new CustomerDto { Id = 502, Name = "Maria Gonzales", Email = "maria@example.com", Country = "Argentina" },
                new CustomerDto { Id = 503, Name = "Luisa Miller", Email = "luisa@example.com", Country = "Estados Unidos" }
             };

                 return Ok(customers);
        }
    }
}
