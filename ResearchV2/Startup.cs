using System;
using System.IO;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ResearchV2
{
    public class Startup
    {
        public Startup()
        {
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHostedService<Worker>();
            services.AddLogging();
            var connectString = "Host=localhost;Database=StockResearch;Username=postgres;Password=13";
            services.AddHangfire(x => x.UsePostgreSqlStorage(connectString));
            services.AddHangfireServer();
        }

        public void Configure(IApplicationBuilder app) {
            app.UseHangfireDashboard();
            app.Use((context, next) =>
            {
                return next().ContinueWith(result =>
                {
                    Console.WriteLine("Scheme {0} : Method {1} : Path {2} : MS {3}",
                    context.Request.Scheme, context.Request.Method, context.Request.Path, getTime());
                });
            });

            app.Run(async context =>
            {
                await context.Response.WriteAsync(getTime() + " My First OWIN App");
            });
        }

        string getTime()
        {
            return DateTime.Now.Millisecond.ToString();
        }
    }
}
