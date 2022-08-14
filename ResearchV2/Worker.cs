using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Hosting;

namespace ResearchV2
{
    public class Worker : BackgroundService
    {
        public Worker()
        {
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            BackgroundJob.Enqueue(() => Console.WriteLine("Simple!"));
            return Task.CompletedTask;
        }
    }
}
