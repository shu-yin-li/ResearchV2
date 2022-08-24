using System;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace ResearchWebApi
{
    public class ScopedJobActivator: JobActivator
    {
        private readonly IServiceProvider _serviceProvider;
        public ScopedJobActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public override object ActivateJob(Type jobType)
        {
            var a = _serviceProvider.GetService(jobType);
            var b = base.ActivateJob(jobType);
            return _serviceProvider.GetService(jobType);
        }
    }
}
