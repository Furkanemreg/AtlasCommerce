using AutoMapper;
using AtlasCommerce.Application.Interfaces;
using AtlasCommerce.Application.Mapping;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application
{
    public static class ServiceRegistration
    {
        public static void AddApplicationServices(this IServiceCollection services)
        {
            var mappingConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new GeneralMapping());
                cfg.AddProfile(new UserMapping());
                cfg.AddProfile(new CategoryMapping());
                cfg.AddProfile(new MessageMapping());
                cfg.AddProfile(new ProductMapping());
                cfg.AddProfile(new SettingMapping());
            });

            IMapper mapper = mappingConfig.CreateMapper();
            services.AddSingleton(mapper);
            // Service Registrations

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = new Microsoft.AspNetCore.Http.PathString("/Admin/Account/Login");
                options.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.AccessDeniedPath = new Microsoft.AspNetCore.Http.PathString("/Admin/Account/Login");
            });
        }

    }
}
