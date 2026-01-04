using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Interfaces
{
    public interface ICurrentUserService
    {
        Guid UserId();
        string FirstName();
        string LastName();
        string UserName();
        string EmailAddress();
        string PhoneNumber();
    }
}
