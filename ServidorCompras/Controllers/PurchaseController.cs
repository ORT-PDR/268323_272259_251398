using Domain;
using Microsoft.AspNetCore.Mvc;
using WebApiRabbitMQ.Data;

namespace WebApiRabbitMQ.Controllers
{
    [ApiController]
    [Route("api/purchases")]
    public class PurchaseController : ControllerBase
    {

        private readonly ILogger<PurchaseController> _logger;

        public PurchaseController(ILogger<PurchaseController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<Purchase> Get([FromQuery] string? userName, [FromQuery] string? productName, [FromQuery] string? purchaseDate)
        {
            IEnumerable<Purchase> purchases =  PurchaseDataAccess.GetInstance().GetPurchases();
            if (!string.IsNullOrEmpty(userName))
            {
                purchases = purchases.Where(p => p.UserName == userName);
            }

            if (!string.IsNullOrEmpty(productName))
            {
                purchases = purchases.Where(p => p.Product == productName);
            }

            if (!string.IsNullOrEmpty(purchaseDate))
            {
                purchases = purchases.Where(p => p.PurchaseDate == purchaseDate);
            }

            return purchases;

        }

        
    }
}
