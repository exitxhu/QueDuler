using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QueDuler.Helpers
{
    public static class DiExtension
    {

        public static IServiceCollection AddQueduler(this IServiceCollection services, Action<QuedulerOptions> configuration)
        {
            services.AddSingleton<Dispatcher>();

            var config = new QuedulerOptions();
            configuration(config);

            List<Type> dispatches = new();
            List<Type> schedules = new();
            List<Type> observables= new();
            foreach (var assembly in config.JobAssemblies)
            {
                dispatches.AddRange(assembly.DefinedTypes
                    .Where(n => n.ImplementedInterfaces.Contains(typeof(IDispatchableJob)))
                    .Select(n => n.AsType()));
                schedules.AddRange(assembly.DefinedTypes
                   .Where(n => n.ImplementedInterfaces.Contains(typeof(ISchedulableJob)))
                   .Select(n => n.AsType()));
                observables.AddRange(assembly.DefinedTypes
                   .Where(n => n.ImplementedInterfaces.Contains(typeof(IObservableJob)))
                   .Select(n => n.AsType()));
            }

            foreach (var type in dispatches.Concat(schedules).Concat(observables))
            {
                services.AddTransient(type);
            }
            var args = new DispatcherArg
            {
                DispatchableJobs = dispatches,
                SchedulableJobs = schedules,
                ObservableJobs = observables,
            };
            services.AddSingleton(args);
            services.AddScoped<JobResolver>();

            return services;
        }
        public static QuedulerOptions AddJobAssemblies(this QuedulerOptions options, params Type[] types)
        {
            options.JobAssemblies = types.Select(n => n.Assembly).Distinct().ToList();
            return options;
        }
        public static QuedulerOptions AddJobAssemblies(this QuedulerOptions options, params Assembly[] assemblies)
        {
            options.JobAssemblies = assemblies.ToList();
            return options;
        }
    }
    public class QuedulerOptions
    {
        public List<Assembly> JobAssemblies { get; set; } = new List<Assembly>();
    }
    public class DispatcherArg
    {
        public IEnumerable<Type> DispatchableJobs { get; set; }
        public IEnumerable<Type> SchedulableJobs { get; set; }
        public IEnumerable<Type> ObservableJobs { get; set; }
    }
}
