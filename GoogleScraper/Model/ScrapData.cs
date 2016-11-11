using System.Collections.Generic;
using System.ComponentModel;

namespace GoogleScraper
{
    public class ScrapeData 
    {
        [DisplayName("Web site url")]
        public string WebsiteUrl { get; set; }

        [DisplayName("Root")]
        public string Root { get; set; }

        [DisplayName("Email")]
        public string Email { get; set; }

        [DisplayName("Phone number")]
        public string PhoneNumber { get; set; }

        [DisplayName("Company name")]
        public string CompanyName { get; set; }
      
    }
}
