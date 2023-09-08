using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servidor
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool Connected { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is Usuario otherUsuario)
            {
                return this.Id == otherUsuario.Id;
            }
            return false;
        }

    }
}
