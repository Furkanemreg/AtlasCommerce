using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasCommerce.Application.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);

                // Eğer 404 ise ve henüz response gönderilmemişse
                if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
                {
                    context.Response.Clear();
                    context.Response.Redirect("/Home/Error");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occured: {ex}");
                Log.Error(ex, "BAn error occured.");
                if (!context.Response.HasStarted)
                {
                    context.Response.Clear();
                    context.Response.Redirect("/Home/Error");
                }
            }
        }
    }

}
