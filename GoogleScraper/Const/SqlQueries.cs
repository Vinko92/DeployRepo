using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleScraper.Const
{
    public static class SqlQueries
    {

        public const string CREATE_TABLE = @"CREATE TABLE URLS (WebsiteUrl string, Root string, Email string, PhoneNumber string, CompanyName string)";

        public const string DROP_TABLE = @"DROP TABLE [URLS]";

        public const string INSERT = "INSERT INTO URLS(WebsiteUrl, Root, Email, PhoneNumber,CompanyName) VALUES('{0}', '{1}', '{2}', '{3}', '{4}');";

    }
}
