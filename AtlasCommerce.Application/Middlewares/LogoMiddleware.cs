using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Middlewares
{
    public class LogoMiddleware
    {
        readonly RequestDelegate _next;

        public LogoMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var domain = context.Request.Host.Host;
            var config = context.RequestServices.GetRequiredService<IConfiguration>();
            var logos = config.GetSection("Logos").Get<Dictionary<string, string>>();

            if (logos.ContainsKey(domain))
            {
                context.Items["Logo"] = logos[domain];
            }
            else
            {
                context.Items["Logo"] = logos["default"];
            }

            await _next(context);
        }
    }
}
