using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Protocolo
{
    public class FileCommsHandler
    {
        private readonly ConversionHandler _conversionHandler;
        private readonly SocketHelper _socketHelper;
        private static object locker = new object();
        public FileCommsHandler(Socket socket)
        {
            _conversionHandler = new ConversionHandler();
            //_socketHelper = socketHelper;
            _socketHelper = new SocketHelper(socket);
        }

        public void SendFile(string path,string destinationName)
        {
            if (FileHandler.FileExists(path))
            {
                var fileName = FileHandler.GetFileName(path);
                // ---> Enviar el largo del nombre del archivo
                _socketHelper.Send(_conversionHandler.ConvertIntToBytes(destinationName.Length));
                // ---> Enviar el nombre del archivo
                _socketHelper.Send(_conversionHandler.ConvertStringToBytes(destinationName));

                // ---> Obtener el tamaño del archivo
                long fileSize = FileHandler.GetFileSize(path);
                // ---> Enviar el tamaño del archivo
                var convertedFileSize = _conversionHandler.ConvertLongToBytes(fileSize);
                _socketHelper.Send(convertedFileSize);
                // ---> Enviar el archivo (pero con file stream)
                SendFileWithStream(fileSize, path);
            }
            else
            {
                throw new Exception("File does not exist");
            }
        }

        public void ReceiveFile(string fileRoute)
        {
            // ---> Recibir el largo del nombre del archivo
            int fileNameSize = _conversionHandler.ConvertBytesToInt(
                _socketHelper.Receive(Protocol.DataSize));
            // ---> Recibir el nombre del archivo
            string fileName = _conversionHandler.ConvertBytesToString(_socketHelper.Receive(fileNameSize));
            Console.WriteLine(fileName);
            fileName = fileRoute + @"\" + fileName;

            // ---> Recibir el largo del archivo
            long fileSize = _conversionHandler.ConvertBytesToLong(
                _socketHelper.Receive(Protocol.FileSize));
            // ---> Recibir el archivo
            ReceiveFileWithStreams(fileSize, fileName);
        }

        private void SendFileWithStream(long fileSize, string path)
        {
            long fileParts = Protocol.CalculateFileParts(fileSize);
            long offset = 0;
            long currentPart = 1;

            //Mientras tengo un segmento a enviar
            while (fileSize > offset)
            {
                byte[] data;
                //Es el último segmento?
                if (currentPart == fileParts)
                {
                    var lastPartSize = (int)(fileSize - offset);
                    //1- Leo de disco el último segmento
                    //2- Guardo el último segmento en un buffer
                    data = FileStreamHandler.Read(path, offset, lastPartSize); //Puntos 1 y 2
                    offset += lastPartSize;
                }
                else
                {
                    //1- Leo de disco el segmento
                    //2- Guardo ese segmento en un buffer
                    data = FileStreamHandler.Read(path, offset, Protocol.MaxPacketSize);
                    offset += Protocol.MaxPacketSize;
                }

                _socketHelper.Send(data); //3- Envío ese segmento a travez de la red
                currentPart++;
            }
        }

        private void ReceiveFileWithStreams(long fileSize, string fileName)
        {
            long fileParts = Protocol.CalculateFileParts(fileSize);
            long offset = 0;
            long currentPart = 1;

            //Mientras tengo partes para recibir
            while (fileSize > offset)
            {
                byte[] data;
                //1- Me fijo si es la ultima parte
                if (currentPart == fileParts)
                {
                    //1.1 - Si es, recibo la ultima parte
                    var lastPartSize = (int)(fileSize - offset);
                    data = _socketHelper.Receive(lastPartSize);
                    offset += lastPartSize;
                }
                else
                {
                    //2.2- Si no, recibo una parte cualquiera
                    data = _socketHelper.Receive(Protocol.MaxPacketSize);
                    offset += Protocol.MaxPacketSize;
                }
                //3- Escribo esa parte del archivo a disco
                lock (locker)
                {
                    FileStreamHandler.Write(fileName, data);
                }
               
                currentPart++;
            }
        }
    }
}
