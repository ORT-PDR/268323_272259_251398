using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servidor
{
    internal class Producto
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Stock { get; set; }
        public int Price { get; set; }
        public string Image { get; set; }
        public override bool Equals(object? obj)
        {
            if (obj is Producto otherProducto)
            {
                return this.Name == otherProducto.Name;
            }
            return false;
        }
    }
}
