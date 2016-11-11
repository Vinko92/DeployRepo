using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using GoogleScraper.Const;
using HtmlAgilityPack;

namespace GoogleScraper.Parser
{
    public static class HtmlParser
    {
        public static List<ScrapeData> ParseUrls(string rawText)
        {
            List<ScrapeData> data = new List<ScrapeData>();
            HtmlNodeCollection results = ParseResults(rawText);

            if (results != null)
            {
                foreach (var result in results)
                {
                    string uri = GetUri(result.InnerHtml);
                    string root = GetDomain(uri);

                    if (string.IsNullOrEmpty(root)) root = GetDomain("www." + uri);

                    data.Add(new ScrapeData
                    {
                        WebsiteUrl = uri,
                        Root = root
                    });
                }
            }

            return data;
        }

        public static string ParseEmail(string rawText,IEnumerable<string> emailPaterns)
        {
            string email = string.Empty;
            Regex regex;

            if (emailPaterns.Count() > 0)
            {
                List<string> paterns = new List<string>();

                foreach(var patern in emailPaterns)
                {
                    paterns.Add(string.Format(RegexPaterns.EMAIL_REGEX_FORMAT, patern.Trim()));
                }

                var regexPatern = "(" + string.Join("|", paterns) + ")";
                regex = new Regex(regexPatern);
            }
            else
            {
                regex = new Regex(RegexPaterns.EMAIL_REGEX);
            }

            MatchCollection matches = regex.Matches(rawText);

            foreach (Match match in matches)
            {
                var value = match.Value;

                if (value.EndsWith(".png") || value.EndsWith(".jpg") || value.EndsWith(".jpeg"))
                    continue;
                else
                {
                    email = value;
                    break;
                }
            }
           

            return email;
        }

        public static string ParsePhone(string rawText)
        {
            string email = string.Empty;
            Regex regex = new Regex(RegexPaterns.PHONE_NUMBER_REGEX);
            Match match = regex.Match(rawText);

            if (match.Success)
                email = match.Value;

            return email;
        }

        public static string ParseCompanyName(string rawText)
        {
            string result = string.Empty;

            var document = new HtmlDocument();
            document.LoadHtml(rawText);

            var title = document.DocumentNode.SelectNodes("//title");

            if(title != null)
            {
                result = title[0].InnerText;
            }

            return result;
        }

        private static HtmlNodeCollection ParseResults(string rawText)
        {
            var document = new HtmlDocument();
            document.LoadHtml(rawText);

            return document.DocumentNode.SelectNodes("//cite");
        }

        private static string GetUri(string uriRawText)
        {
            return HttpUtility.UrlDecode(Regex.Replace(uriRawText, "<.*?>", String.Empty));
        }

        private static string GetDomain(string uri)
        {
            return Regex.Match(uri, @"(?:http:\/\/|www\.|https:\/\/)([^\/]+)").Value;
        }
    }
}
