// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Logging;

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
        var det = DispatcherMemMapper.GetJob(path);
        var job = det?.TightJobs?.SingleOrDefault(a => a.JobId == jobId)?.GetType();
        var service = _provider.GetService(job) as IDispatchableJob;

        return service;
    }
}
