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
            if (obj is Usuario otherUser)
            {
                return this.Username == otherUser.Username && this.Password == otherUser.Password;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (this.Username?.GetHashCode() ?? 0);
                hash = hash * 23 + (this.Password?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
