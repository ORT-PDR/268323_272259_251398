﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servidor
{
    internal class Review
    {
        public string UserName { get; set; }

        public string Comment { get; set; }

        public int Rating { get; set; }

        public override string ToString()
        {
            return  "\n     Rating: " + this.Rating +
                    "\n     Comentario: " + this.Comment +
                    "\n     Usuario: " + this.UserName;
        }
    } 
}
