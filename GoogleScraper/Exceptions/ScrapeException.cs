using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleScraper.Exceptions
{
    public class ScrapeException : Exception
    {
        public ScrapeException(string message) : base(message)
        {

        }

    }
}
