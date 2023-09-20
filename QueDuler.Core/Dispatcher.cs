// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QueDuler.Core;
using QueDuler.Helpers;
using System.Collections.Concurrent;
using static JobCache;

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
                using var pro = _provider.CreateScope();

                var j = pro.ServiceProvider.GetService(job) as ISchedulableJob
                    ?? throw new JobNotInjectedException(job.FullName);
                JobCache.AddSchedulable(j);
                _scheduler.Schedule(j);
            }
        }
        if (_broker is not null)
        {
            foreach (var job in _jobs.DispatchableJobs)
            {
                using var pro = _provider.CreateScope();

                var j = pro.ServiceProvider.GetService(job) as IDispatchableJob
                    ?? throw new JobNotInjectedException(job.FullName);
                dispatchables.Add(j);
            }


            _broker.OnMessageReceived += async (s, a) =>
            {
                try
                {
                    _logger.LogInformation($"Injected OnMessageReceived received a new message received {a}");

                    var det = JobCache.GetDispatchable(a.JobPath);
                    if (det is null)
                    {
                        _logger.LogWarning($"Injected OnMessageReceived  can not found any jobs for the path: {a.JobPath}");
                        return;
                    }

                    var check = DispatchableJobArgument.TryParse(a.Message, out DispatchableJobArgument arg);
                    if (check)
                    {
                        if (det.TightJobs is null || !det.TightJobs.Any())
                        {
                            _logger.LogWarning($"Injected OnMessageReceived (queduler kafka broker) has a message: {a.Message} which is not corresponded with any job at path: {a.JobPath}");
                            return;
                        }
                        if (arg.IsBroadCast)
                        {
                            var jobTasks = det.AllJobs.Select(j => Task.Run(async () =>
                            {
                                var job = j.GetType();
                                await DispatchJob(a, arg, job);
                            }));
                            Task.WaitAll(jobTasks.ToArray());
                        }
                        else
                        {
                            var job = det.TightJobs.SingleOrDefault(a => a.JobId == arg.JobId)?.GetType();
                            await DispatchJob(a, arg, job);
                        }
                    }
                    if (det.LoosJobs is null || !det.LoosJobs.Any())
                    {
                        return;
                    }
                    else
                    {

                        var jobTasks = det.LoosJobs.Select(j => Task.Run(async () =>
                        {
                            var job = j.GetType();
                            await DispatchJob(a, default, job);
                        }));
                        Task.WaitAll(jobTasks.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    //Todo: something about it
                    _logger.LogCritical(ex, $"Injected OnMessageReceived (queduler kafka broker) encountered an error: {ex.Message}");
                }
            };
            foreach (var jobs in dispatchables.GroupBy(a => a.JobPath))
            {
                JobDets det = new();

                det.AllJobs = jobs.ToList();
                det.LoosJobs = jobs.Where(n => n.LoosArgument).ToList();
                det.TightJobs = jobs.Where(n => !n.LoosArgument).ToList();
                JobCache.AddDispatchable(jobs.Key, det);
            }
            Task.Run(() => _broker.StartConsumingAsyn(cancellationToken)).ConfigureAwait(false);

        }

        async Task DispatchJob(OnMessageReceivedArgs a, DispatchableJobArgument arg, Type? job)
        {
            using var pro = _provider.CreateScope();
            var service = pro.ServiceProvider.GetService(job) as IDispatchableJob;
            if (service is not null)
            {
                await service.Dispatch(arg, a.originalMessage);
            }
            else
                _logger.LogWarning($"Injected OnMessageReceived (queduler kafka broker) has a message: {a.Message} which is not corresponded with any job at path: {a.JobPath}");

        }
    }
}
public static class JobCache
{
    static ConcurrentDictionary<string, JobDets> _dispactmem = new();
    static ConcurrentDictionary<string, ISchedulableJob> _schedmem = new();
    public static void AddDispatchable(string key, JobDets value)
    {
        _dispactmem.TryAdd(key, value);
    }
    public static JobDets GetDispatchable(string key)
    {
        _dispactmem.TryGetValue(key, out var v);
        return v;
    }
    public static void AddSchedulable(ISchedulableJob value)
    {
        _schedmem.TryAdd(value.JobId, value);
    }
    public static ISchedulableJob GetSchedulable(string key)
    {
        _schedmem.TryGetValue(key, out var v);
        return v;
    }
    public class JobDets
    {
        public List<IDispatchableJob>? TightJobs { get; set; }
        public List<IDispatchableJob>? LoosJobs { get; set; }
        public List<IDispatchableJob> AllJobs { get; internal set; }
    }
}
