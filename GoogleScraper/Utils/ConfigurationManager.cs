using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleScraper.Utils
{
    public static class Config
    {
        public static string GetOleDbConnectionString(string filePath)
        {
            return string.Format(ConfigurationManager.AppSettings["OleDbConnection"], filePath);
        }

        public static string GetAutosavePath()
        {
            return ConfigurationManager.AppSettings["AutosavePath"];
        }
    }
}
