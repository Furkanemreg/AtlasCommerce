using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Attributes
{

    [AttributeUsage(AttributeTargets.Class)]
    public class IgnoreCompanyFilterAttribute : Attribute
    {
    }
}
