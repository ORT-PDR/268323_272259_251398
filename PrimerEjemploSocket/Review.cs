using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servidor
{
    internal class Review
    {
        public int UserId { get; set; }

        public string Comment { get; set; }

        public int Rating { get; set; }

        public int minRating { get; set; }

        public int maxRating { get; set; }
        //public int TotalRatingSum { get; internal set; }
        //public int AmountOfRatings { get; internal set; }

        public override string ToString()
        {
            return  "\n     Rating: " + this.Rating +
                    "\n     Comentario: " + this.Comment +
                    "\n     Usuario: " + this.UserId;
        }
    } 
}
