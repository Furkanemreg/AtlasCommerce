using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.DTOs
{
    public class UpdateUserSettingsDto
    {
        public string Name { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }    // Yeni şifre

        // Modelden gelecek
        public string CurrentPassword { get; set; }
    }
}
