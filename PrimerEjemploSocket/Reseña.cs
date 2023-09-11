using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servidor
{
    internal class Reseña
    {
        public int UserId { get; set; }

        public string Comment { get; set; }

        public int Rating { get; set; }

        public int minRating { get; set; }

        public int maxRating { get; set; }
    }
}
