using Domain;

namespace WebApiRabbitMQ.Data
{
    public class PurchaseDataAccess
    {
        private List<Purchase> purchases;
        private object padlock;
        private static PurchaseDataAccess instance;

        private static object singletonPadlock = new object();
        public static PurchaseDataAccess GetInstance() {

            lock (singletonPadlock) { // bloqueante 
            if (instance == null) {
                instance = new PurchaseDataAccess();
            }
            }
            return instance;
        }

        private PurchaseDataAccess() {
            purchases = new List<Purchase>();
            padlock = new object();
        }

        public void AddPurchase(Purchase purchase) {
            lock (padlock) 
            {
                purchases.Add(purchase);
            }
        }

        public List<Purchase> GetPurchases() {
            lock (padlock) { 
            return purchases;
            }
        }

    }
}
