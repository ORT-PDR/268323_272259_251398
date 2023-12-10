
using MailServer.Service;

namespace MailServer
{
    public class Program
    {
        private static MQServiceMailServer mq;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            mq = new MQServiceMailServer();
            StartMailServer(mq);
        }


        public static async Task StartMailServer(MQServiceMailServer mq)
        {
            Console.WriteLine("Server will start listening to purchase queue");
         //   await Task.Run(() => mq.HandleQueue());
        }
    }
}