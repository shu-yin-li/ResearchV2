using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResearchV2;
using Microsoft.AspNetCore.Hosting;

namespace Stock.Analysis._0607
{
    class Program
    {

        public static readonly ILoggerFactory MyLoggerFactory =
           LoggerFactory.Create(
                builder =>
                {
                    builder.AddConsole().AddFilter(level => level == LogLevel.Critical);
                }
           );

        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();

        }
        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webbuilder => webbuilder.UseStartup<Startup>());
    }
}
