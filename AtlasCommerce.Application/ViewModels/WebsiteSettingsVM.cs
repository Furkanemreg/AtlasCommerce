using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.ViewModels
{
    public class WebsiteSettingsVM
    {
        public Guid Id { get; set; }
        public string? BrandName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? EmailAddress { get; set; }
        public string? Address { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? ZipCode { get; set; }
        public string? LogoPath { get; set; }
        public string? FooterText { get; set; }
        public string? Instagram { get; set; }
        public string? Twitter { get; set; }
        public string? Youtube { get; set; }
        public string? AboutParagraph { get; set; }
        public string? AboutAltSentence1 { get; set; }
        public string? AboutAltSentence2 { get; set; }
    }
}

