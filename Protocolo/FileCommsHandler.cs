﻿using Exceptions;
using System.Net.Sockets;

namespace Protocolo
{
    public class FileCommsHandler
    {
        private readonly ConversionHandler _conversionHandler;
        private readonly SocketHelper _socketHelper;
        private static object locker = new object();
        public FileCommsHandler(TcpClient client)
        {
            _conversionHandler = new ConversionHandler();
            _socketHelper = new SocketHelper(client);
        }

        public async Task SendFile(string path,string destinationName)
        {
            if (FileHandler.FileExists(path))
            {
                var fileName = FileHandler.GetFileName(path);
                // ---> Enviar el largo del nombre del archivo
                await _socketHelper.SendAsync(_conversionHandler.ConvertIntToBytes(destinationName.Length));
                // ---> Enviar el nombre del archivo
                await _socketHelper.SendAsync(_conversionHandler.ConvertStringToBytes(destinationName));
                // ---> Obtener el tamaño del archivo
                long fileSize = FileHandler.GetFileSize(path);
                // ---> Enviar el tamaño del archivo
                var convertedFileSize = _conversionHandler.ConvertLongToBytes(fileSize);
                await _socketHelper.SendAsync(convertedFileSize);
                // ---> Enviar el archivo (pero con file stream)
                await SendFileWithStream(fileSize, path);
            }
            else
            {
                throw new NonexistingFileException("File does not exist");
            }
        }

        public async Task ReceiveFile(string path)
        {
            // ---> Recibir el largo del nombre del archivo
            int fileNameSize = _conversionHandler.ConvertBytesToInt(
                await _socketHelper.ReceiveAsync(Protocol.DataSize));
            // ---> Recibir el nombre del archivo
            string fileName = _conversionHandler.ConvertBytesToString(await _socketHelper.ReceiveAsync(fileNameSize));
            Console.WriteLine(fileName);
            // ---> Recibir el largo del archivo
            long fileSize = _conversionHandler.ConvertBytesToLong(
                await _socketHelper.ReceiveAsync(Protocol.FileSize));
            // ---> Recibir el archivo
            await ReceiveFileWithStreams(fileSize, path + @"\" + fileName);
        }

        private async Task SendFileWithStream(long fileSize, string path)
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

                await _socketHelper.SendAsync(data); //3- Envío ese segmento a travez de la red
                currentPart++;
            }
        }

        private async Task ReceiveFileWithStreams(long fileSize, string fileName)
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
                    data = await _socketHelper.ReceiveAsync(lastPartSize);
                    offset += lastPartSize;
                }
                else
                {
                    //2.2- Si no, recibo una parte cualquiera
                    data = await _socketHelper.ReceiveAsync(Protocol.MaxPacketSize);
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
