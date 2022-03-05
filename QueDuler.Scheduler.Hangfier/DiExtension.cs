using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using QueDuler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KafkaDuler.Helpers
{
    public static class DiExtension
    {

        public static QuedulerOptions AddHangfireScheduler(this QuedulerOptions configuration, IServiceCollection services, Action<IGlobalConfiguration> hangfireConfig)
        {
            services.AddHangfire(hangfireConfig);
            services.AddHangfireServer();

            services.AddTransient<IScheduler, HangfireScheduler>();
            return configuration;
        }
    }
}