using AtlasCommerce.Application.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Attributes
{

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class MenuNameAttribute : Attribute
    {
        public MenuNames Name { get; set; }

        public MenuNameAttribute(MenuNames name)
        {
            Name = name;
        }

    }
}
