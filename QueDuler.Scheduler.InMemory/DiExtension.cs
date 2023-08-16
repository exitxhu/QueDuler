using Microsoft.Extensions.DependencyInjection;
using QueDuler.Helpers;


namespace QueDuler.Helpers;

public static class DiExtension
{

    public static QuedulerOptions AddInMemoryScheduler(this QuedulerOptions configuration, IServiceCollection services, InMemorySchedulerOptions options = null)
    {
        services.AddSingleton<MemSched>(a => new MemSched(a, options));

        services.AddTransient<IScheduler, InMemoryScheduler>();
        return configuration;
    }
}
