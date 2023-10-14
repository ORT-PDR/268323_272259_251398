using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Protocolo
{
    public static class FileHandler
    {
        static readonly SettingsManager settingMng = new SettingsManager();

        public static bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public static string GetFileName(string path)
        {
            if (FileExists(path))
            {
                return new FileInfo(path).Name;
            }

            throw new Exception("File does not exist");
        }

        public static long GetFileSize(string path)
        {
            if (FileExists(path))
            {
                return new FileInfo(path).Length;
            }

            throw new Exception("File does not exist");
        }

        public static void DeleteFile(string fileName, string pathDir)
        {
            try
            {
                string searchDirectory = @pathDir;
                // Search for image files with the specified name
                string[] imageFiles = Directory.GetFiles(searchDirectory, $"{fileName}.*");

                 string path = imageFiles[0];
                 File.Delete(path);
            }
            catch { }
            
        }
    }
}
