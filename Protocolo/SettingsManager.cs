using Common.Interfaces;
using System.Collections.Specialized;
using System.Configuration;

namespace Common
{
    public class SettingsManager : ISettingsManager
    {
        public string ReadSettings(string key)
        {
            try
            {
                // var appSettings = ConfigurationManager.AppSettings;
                NameValueCollection appSettings = ConfigurationManager.AppSettings;
                return appSettings[key] ?? string.Empty;
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app settings");
                return string.Empty;
            }
        }

    }
}