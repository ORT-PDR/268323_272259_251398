using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Servidor;
using WebApiRabbitMQ.Data;

namespace WebApiRabbitMQ.Controllers
{
    [ApiController]
    [Route("api/purchases")]
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
        public IEnumerable<Purchase> Get()
        {
            return PurchaseDataAccess.GetInstance().GetPurchases();
        }

        [HttpPost]
        public ActionResult PostPurchase([FromBody] Purchase purchase) 
        {
            //ProgramServidor session = ProgramServidor.Instance;
            //session.AddPurchase(purchase);
            //var reply = "Se realizó la compra con un total de " + purchase.Total;
            PurchaseDataAccess.GetInstance().AddPurchase(purchase);
            var reply = "added purchase of " + purchase.UserName + " with cost of " + purchase.Total;
            return Ok(reply);
        }
    }
}
