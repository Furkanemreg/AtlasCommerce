using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;

namespace AtlasCommerce.Persistance.Helpers
{
    public static class EnumHelper
    {
        public static string GetEnumDescription(Enum value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            var fieldInfo = value.GetType().GetField(value.ToString());

            if (fieldInfo == null)
            {
                return value.ToString();
            }

            var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return (attributes.Length > 0) ? attributes[0].Description : value.ToString();
        }

        public static IEnumerable<SelectListItem> GetEnumSelectListWithDescription<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T))
                       .Cast<T>()
                       .Select(e => new SelectListItem
                       {
                           Value = Convert.ToInt32(e).ToString(),
                           Text = GetEnumDescription(e)
                       });
        }
    }
}

