using System;
using System.Collections.Generic;

namespace GoogleScraper
{
    public class ScrapeDataComparer : IEqualityComparer<ScrapeData>
    {
        public bool Equals(ScrapeData x, ScrapeData y)
        {
            return x.WebsiteUrl.Equals(y.WebsiteUrl);
        }

        public int GetHashCode(ScrapeData obj)
        {
            return obj.WebsiteUrl.GetHashCode();
        }
    }
}
