using System;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResearchWebApi.Interface;
using ResearchWebApi.Models;
using ResearchWebApi.Repository;
using ResearchWebApi.Services;

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

            var connectString = "Host=34.80.90.73;Database=stockresearch;Username=postgres;Password=F7PVvyi1Vegc";
            //var connectString = "Host=localhost;Database=StockResearch;Username=postgres;Password=13";

            // DI
            services.AddScoped<IResearchOperationService, ResearchOperationService>();
            services.AddScoped<ICalculateVolumeService, CalculateVolumeService>();
            services.AddScoped<IDataService, DataService>();
            services.AddScoped<IIndicatorCalulationService, IndicatorCalculationService>();
            services.AddScoped<IOutputResultService, OutputResultService>();
            services.AddScoped<IJobsService, JobsService>();
            services.AddScoped<ISlidingWindowService, SlidingWindowService>();
            services.AddScoped<ITransTimingService, TransTimingService>();
            services.AddScoped<IGNQTSAlgorithmService, TrailingStopGNQTSAlgorithmService>();
            services.AddScoped<IFileHandler, FileHandler>();
            
            // DB
            services.AddDbContextPool<StockModelDbContext>(options => { options.UseNpgsql(connectString).UseLoggerFactory(MyLoggerFactory);});
            services.AddScoped<IStockModelDataProvider, StockModelDataProvider>();
            services.AddDbContextPool<CommonResultDbContext>(options => options.UseNpgsql(connectString).UseLoggerFactory(MyLoggerFactory));
            services.AddScoped<IDataProvider<CommonResult>, CommonResultDataProvider>();
            services.AddDbContextPool<EarnResultDbContext>(options => options.UseNpgsql(connectString).UseLoggerFactory(MyLoggerFactory));
            services.AddScoped<IDataProvider<EarnResult>, EarnResultDataProvider>();
            services.AddDbContextPool<TrainDetailsDbContext>(options => options.UseNpgsql(connectString).UseLoggerFactory(MyLoggerFactory));
            services.AddScoped<ITrainDetailsDataProvider, TrainDetailsDataProvider>();

            // Hangfire
            services.AddTransient<ScopedJobActivator>();
            services.AddHangfire((serviceProvider, config) => {
                var scopedProvider = serviceProvider.CreateScope().ServiceProvider;
                config
                    //.UseActivator(new ScopedJobActivator(scopedProvider))
                    .UsePostgreSqlStorage(connectString);
            });
            services.AddHangfireServer(options => options.WorkerCount = 20);

            services.AddSwaggerGen();

            // AutoMapper
            services.AddAutoMapper(typeof(Startup));
            services.AddAutoMapper(typeof(StockModel));
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
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new MyAuthorizationFilter() }
            });
            app.Use((context, next) =>
            {
                return next().ContinueWith(result =>
                {
                    Console.WriteLine("Scheme {0} : Method {1} : Path {2} : MS {3}",
                    context.Request.Scheme, context.Request.Method, context.Request.Path, getTime());
                });
            });

            // swagger
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        string getTime()
        {
            return DateTime.Now.Millisecond.ToString();
        }

        public readonly ILoggerFactory MyLoggerFactory =
           LoggerFactory.Create(
                builder =>
                {
                    builder.AddConsole().AddFilter(level => level == LogLevel.Critical);
                }
           );
    }
}
