using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class Product
    {
        public int Id { get; set; }
        public string OwnerUserName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Stock { get; set; }
        public int Price { get; set; }
        public string Image { get; set; }
        public List<Review> Reviews { get; set; }

        public static int id = 0;


        public Product() {
            this.Reviews = new List<Review>();
            Id = id;
            id++;
        }

        public override string ToString()
        {
            string reviews = "";
            for (int i = 0; i < Reviews.Count; i++)
            {
                reviews += Reviews[i].ToString();
                reviews += "\n --------------------- \n ";
            }
            return "\nId: " + this.Id +
                "\nId Dueño: " + this.OwnerUserName +
                "\nNombre: " + this.Name +
                "\nDescripción: " + this.Description +
                "\nStock: " + this.Stock +
                "\nPrecio: $" + this.Price +
                "\nImagen: " + this.Image +
                "\nReviews: " + reviews;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Product otherProducto)
            {
                return this.Name == otherProducto.Name;
            }
            return false;
        }
    }
}
