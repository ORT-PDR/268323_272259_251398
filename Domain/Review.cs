namespace Domain
{
    public class Review
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
