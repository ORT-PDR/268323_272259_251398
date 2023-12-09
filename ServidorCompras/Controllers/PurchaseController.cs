using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebApiRabbitMQ.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PurchaseController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<PurchaseController> _logger;

        public PurchaseController(ILogger<PurchaseController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            return ForecastDataAccess.GetInstance().GetForecasts();
        }
    }
}
