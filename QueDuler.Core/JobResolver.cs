// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Logging;
using QueDuler.Helpers;

/// <summary>
/// Resolve any jobs that registered
/// </summary>
public sealed class JobResolver
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<JobResolver> _logger;
    public JobResolver(ILogger<JobResolver> logger, IServiceProvider provider)
    {
        _logger = logger;
        _provider = provider;
    }

    public IDispatchableJob? GetDispatchable(string path, string jobId)
    {
        var det = JobCache.GetDispatchable(path);
        var job = det?.AllJobs?.SingleOrDefault(a => a.JobId == jobId)?.GetType();
        var service = job is null ? null : _provider.GetService(job) as IDispatchableJob;

        return service;
    }
    public ISchedulableJob? GetSchedulable(string jobId)
    {
        var job = JobCache.GetSchedulable(jobId)?.GetType();
       // var job = det?.AllJobs?.SingleOrDefault(a => a.JobId == jobId)?.GetType();
        var service = job is null ? null : _provider.GetService(job) as ISchedulableJob;

        return service;
    }
}
