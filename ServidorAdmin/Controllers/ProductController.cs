using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Domain;
using Servidor;

namespace ServidorAdmin.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductController : Controller
    {
        private ProgramServidor _server;

        [HttpPost]
        public ActionResult Create([FromBody] Product product)
        {
            try
            {
                _server = ProgramServidor.Instance;
            }
            catch (ExitException ex)
            {
                return StatusCode(503, ex.Message);
            }
            Product result = _server.CreateProduct(product);
            return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
        }
    }
}
