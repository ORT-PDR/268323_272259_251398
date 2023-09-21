using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servidor
{
    internal class Product
    {
        public int Id { get; set; }
        public string OwnerUserName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Stock { get; set; }
        public int Price { get; set; }
        public string Image { get; set; }
        public List<Review> Reviews { get; set; }

        public Product() {
            this.Reviews = new List<Review>();
        }

        public override string ToString()
        {
            return "\nId: " + this.Id +
                "\nDueño: " + this.OwnerUserName +
                "\nNombre: " + this.Name +
                "\nDescripción: " + this.Description +
                "\nStock: " + this.Stock +
                "\nPrecio: $" + this.Price;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Product otherProducto)
            {
                return this.Name.ToLower() == otherProducto.Name.ToLower();
            }
            return false;
        }
    }
}
