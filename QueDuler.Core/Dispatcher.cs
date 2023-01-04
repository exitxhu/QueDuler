// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QueDuler.Core;
using QueDuler.Helpers;

public partial class Dispatcher
{
    private readonly IBroker? _broker;
    private readonly IScheduler? _scheduler;
    private readonly DispatcherArg _jobs;
    private readonly IServiceProvider _provider;
    private readonly ILogger<Dispatcher> _logger;
    public Dispatcher(IBroker broker, IScheduler scheduler, DispatcherArg jobs, IServiceProvider provider, ILogger<Dispatcher> logger)
    {
        _broker = broker;
        _scheduler = scheduler;
        _jobs = jobs;
        _provider = provider;
        _logger = logger;
    }
    public Dispatcher(IScheduler scheduler, DispatcherArg jobs, IServiceProvider provider, ILogger<Dispatcher> logger)
    {
        _broker = null;
        _scheduler = scheduler;
        _jobs = jobs;
        _provider = provider;
        _logger = logger;
    }
    public Dispatcher(IBroker broker, DispatcherArg jobs, IServiceProvider provider, ILogger<Dispatcher> logger)
    {
        _scheduler = null;
        _broker = broker;
        _jobs = jobs;
        _provider = provider;
        _logger = logger;
    }
    public void Start(CancellationToken cancellationToken)
    {
        HashSet<IDispatchableJob> dispatchables = new();
        HashSet<ISchedulableJob> schedules = new();
        // we need to resolve the jobs inorder to get the jobid from the implentation(in run time)
        // but we should to resolve them again when we want to run them, other wise it wont be thread safe!
        // specially for dispatchables

        if (_scheduler is not null)
        {
            foreach (var job in _jobs.SchedulableJobs)
            {
                var j = _provider.GetService(job) as ISchedulableJob
                    ?? throw new JobNotInjectedException(job.FullName);
                _scheduler.Schedule(j);
            }
        }
        if (_broker is not null)
        {
            foreach (var job in _jobs.DispatchableJobs)
            {
                var j = _provider.GetService(job) as IDispatchableJob
                    ?? throw new JobNotInjectedException(job.FullName);
                dispatchables.Add(j);
            }
            _broker.OnMessageReceived += async (s, a) =>
            {
                try
                {
                    _logger.LogInformation($"Injected OnMessageReceived received a new message received {a}");

                    var check = DispatchableJobArgument.TryParse(a.Message, out DispatchableJobArgument arg);
                    if (check)
                    {
                        var jobs = dispatchables.Where(n => n.JobPath == a.JobPath).ToList();
                        if (jobs is null || !jobs.Any())
                        {
                            _logger.LogWarning($"Injected OnMessageReceived (queduler kafka broker) has a message: {a.Message} which is not corresponded with any job at path: {a.JobPath}");
                            return;
                        }
                        if (arg.IsBroadCast)
                        {
                            var jobTasks = jobs.Select(j => Task.Run(async () =>
                            {
                                var job = j.GetType();
                                await DispatchJob(a, arg, job);
                            }));
                            Task.WaitAll(jobTasks.ToArray());
                        }
                        else
                        {
                            var job = jobs.SingleOrDefault(a => a.JobId == arg.JobId)?.GetType();
                            await DispatchJob(a, arg, job);
                        }
                    }
                }
                catch (Exception ex)
                {
                    //Todo: something about it
                    _logger.LogCritical(ex, $"Injected OnMessageReceived (queduler kafka broker) encountered an error: {ex.Message}");
                }
            };
            Task.Run(() => _broker.StartConsumingAsyn(cancellationToken)).ConfigureAwait(false);
        }

        async Task DispatchJob(OnMessageReceivedArgs a, DispatchableJobArgument arg, Type? job)
        {
            var service = _provider.CreateScope().ServiceProvider.GetService(job) as IDispatchableJob;
            if (service is not null)
            {
                await service.Dispatch(arg);
            }
            else
                _logger.LogWarning($"Injected OnMessageReceived (queduler kafka broker) has a message: {a.Message} which is not corresponded with any job at path: {a.JobPath}");
        }
    }
}
