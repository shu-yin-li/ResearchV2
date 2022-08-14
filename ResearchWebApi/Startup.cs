using System;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ResearchWebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddLogging();
            var connectString = "Host=localhost;Database=StockResearch;Username=postgres;Password=13";
            services.AddHangfire(x => x.UsePostgreSqlStorage(connectString));
            services.AddHangfireServer();
            services.AddSwaggerGen();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Hangfire
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
                await context.Response.WriteAsync(getTime() + " My OWIN App");
            });

            // swagger
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        string getTime()
        {
            return DateTime.Now.Millisecond.ToString();
        }
    }
}
