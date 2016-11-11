using System;
using System.Net;
using System.Threading.Tasks;

using GoogleScraper.Exceptions;

namespace GoogleScraper.Worker
{
    public class ScrapeWorker
    {
        public string ScrapeUrlsAsync(Uri uri)
        {
            string value = string.Empty;

            using (var client = new WebClient())
            {
                try
                {
                    //client.Proxy = new WebProxy("54.255.211.131", 8118);
                    value = client.DownloadString(uri);
                }
                catch (WebException ex)
                {
                    if (ex.Message.Contains("503"))
                    {
                        throw new ScrapeException("Service unavailable. Scraping too fast.");
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }

            return value;
        }

        public async Task<string> ScrapeDetailsAsync(string uri)
        {
            string validUri = TryBuildUri(uri);
            string value = string.Empty;

            if (string.IsNullOrEmpty(validUri))
            {
                return value;
            }

            using (var client = new WebClient())
            {
                try
                {
                    //client.Proxy = new WebProxy("91.121.42.68", 80);
                    value = await client.DownloadStringTaskAsync(validUri);
                }
                catch (WebException ex)
                {
                    if (ex.Message.Contains("503"))
                    {
                        throw new ScrapeException("Service unavailable. Scraping too fast.");
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }

            return value;
        }

        public string TryBuildUri(string uri)
        {
            Uri result;
            string validUri = string.Empty;

            if (Uri.TryCreate(uri, UriKind.Absolute, out result))
            {
                validUri = uri;
            }
            else if (Uri.TryCreate("http://" + uri, UriKind.Absolute, out result))
            {
                validUri = "http://" + uri;
            }

            return validUri;
        }

    }
}
