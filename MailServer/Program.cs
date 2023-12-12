
using MailServer.Service;

namespace MailServer
{
    public class Program
    {
        private static MQServiceMailServer mq;

        static async Task Main(string[] args)
        {
            mq = new MQServiceMailServer();
            await StartMailServer(mq);
        }


        public static async Task StartMailServer(MQServiceMailServer mq)
        {
            Console.WriteLine("El servidor de mail empezará a escuchar la cola de compras");
            await Task.Run(() => mq.HandleQueue());
        }
    }
}