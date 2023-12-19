// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QueDuler.Core;
using QueDuler.Core.Exceptions;
using QueDuler.Core.Internals;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using static JobCache;

public partial class Dispatcher
{
    private readonly IEnumerable<IBroker> _brokers;
    private readonly IEnumerable<IScheduler> _schedulers;
    private readonly DispatcherArg _jobs;
    private readonly IServiceProvider _provider;
    private readonly ILogger<Dispatcher> _logger;
    public bool HasBrokers => _brokers is not null && _brokers.Any();
    public bool HasSchedulers => _schedulers is not null && _schedulers.Any();
    public Dispatcher(DispatcherArg jobs, IServiceProvider provider, ILogger<Dispatcher> logger)
    {
        _jobs = jobs;
        _provider = provider;
        _logger = logger;
        _brokers = GetBrokers();
        _schedulers = GetSchedulers();
    }
    private IEnumerable<IBroker> GetBrokers() => _provider.GetServices<IBroker>().ToList();
    private IEnumerable<IScheduler> GetSchedulers() => _provider.GetServices<IScheduler>().ToList();
    public void Start(CancellationToken cancellationToken)
    {
        HashSet<IDispatchableJob> dispatchables = new();
        HashSet<ISchedulableJob> schedules = new();
        // we need to resolve the jobs in order to get the jobid from the implantation(in run time)
        // but we should to resolve them again when we want to run them, other wise it wont be thread safe!
        // specially for dispatchable

        if (HasSchedulers)
        {
            foreach (var job in _jobs.SchedulableJobs)
            {
                using var pro = _provider.CreateScope();

                var j = pro.ServiceProvider.GetService(job) as ISchedulableJob
                    ?? throw new JobNotInjectedException(job.FullName);
                JobCache.AddSchedulable(j);
                _schedulers.First().Schedule(j);
            }
        }
        if (HasBrokers)
        {
            ResolveAndCacheDispatchables(dispatchables);
            ResolveAndCacheObservablesFactory(_provider);
            var ob = new ConcurrentDictionary<string, List<IObservableJob>>();
            foreach (IBroker _broker in _brokers)
            {

                _broker.OnMessageReceived += async (s, a) =>
                {
                    ProcessObservables(a, ob);
                    await ProccessForDispatchable(a);
                };
                async Task<bool> ProccessForDispatchable(OnMessageReceivedArgs a)
                {
                    try
                    {
                        _logger.LogInformation($"Injected OnMessageReceived received a new message received {a}");

                        var det = JobCache.GetDispatchable(a.JobPath);
                        if (det is null)
                        {
                            _logger.LogWarning($"Injected OnMessageReceived  can not found any jobs for the path: {a.JobPath}");
                            return false;
                        }

                        var check = DispatchableJobArgument.TryParse(a.Message, out DispatchableJobArgument arg);
                        if (check)
                        {
                            if (det.TightJobs is null || !det.TightJobs.Any())
                            {
                                _logger.LogWarning($"Injected OnMessageReceived (queduler kafka broker) has a message: {a.Message} which is not corresponded with any job at path: {a.JobPath}");
                                return false;
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
                            return false;
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
                        return false;
                    }
                    return true;
                }
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

        }

        async Task DispatchJob(OnMessageReceivedArgs a, DispatchableJobArgument arg, Type? job)
        {
            var service = _provider.CreateScope().ServiceProvider.GetService(job) as IDispatchableJob;
            if (service is not null)
            {
                await service.Dispatch(arg, a.originalMessage);
            }
            else
                _logger.LogWarning($"Injected OnMessageReceived (queduler kafka broker) has a message: {a.Message} which is not corresponded with any job at path: {a.JobPath}");

        }

        void ResolveAndCacheDispatchables(HashSet<IDispatchableJob> dispatchables)
        {
            foreach (var job in _jobs.DispatchableJobs)
            {
                var j = _provider.CreateScope().ServiceProvider.GetService(job) as IDispatchableJob
                    ?? throw new JobNotInjectedException(job.FullName);
                dispatchables.Add(j);
            }
        }

        void ResolveAndCacheObservablesFactory(IServiceProvider srv)
        {
            var con = new ConcurrentDictionary<string, List<Type>>();
            foreach (var job in _jobs.ObservableJobs)
            {

                var j = srv.GetService(job) as IObservableJob
                    ?? throw new JobNotInjectedException(job.FullName);
                if (con.TryGetValue(j.JobPath, out var jobs))
                {
                    jobs.Add(job);
                }
                else
                {
                    con.TryAdd(j.JobPath, new List<Type> { job });
                }
            }
            JobCache.SetObservableFactory((a) =>
            {
                var c = con.TryGetValue(a, out var jobs);
                if (!c) return default;
                var o = jobs.Select(job => srv.GetService(job) as IObservableJob
                    ?? throw new JobNotInjectedException(job.FullName)).ToList();
                return o;
            });

        }
    }

    private static void ProcessObservables(OnMessageReceivedArgs a, ConcurrentDictionary<string, List<IObservableJob>> ob)
    {
        var t = ob.TryGetValue(a.ConsumerId, out var obs);
        if (!t)
        {
            obs = JobCache.GetObservable(a.JobPath);
            if (obs is null || obs.Count == 0)
                return;
            ob.TryAdd(a.ConsumerId, obs);
        }

        var tsks = obs?.Select(j => Task.Run(async () =>
        {
            try
            {
                await j.OnNext(a.Message);

            }
            catch (Exception ex)
            {
                await j.OnError(ex);
            }
        }));
        if (tsks?.Any() != true)
            return;
        Task.WaitAll(tsks.ToArray());
    }
}
public static class JobCache
{
    static Func<string, List<IObservableJob>> _observmem = null;
    static ConcurrentDictionary<string, JobDets> _dispactmem = new();
    static ConcurrentDictionary<string, ISchedulableJob> _schedmem = new();
    public static void AddDispatchable(string key, JobDets value)
    {
        _dispactmem.TryAdd(key, value);
    }
    public static void SetObservableFactory(Func<string, List<IObservableJob>> value)
    {
        _observmem = value;
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
    public static List<IObservableJob> GetObservable(string key)
    {
        if (_observmem is null) return default;
        var v = _observmem(key);
        return v;
    }
    public class JobDets
    {
        public List<IDispatchableJob>? TightJobs { get; set; }
        public List<IDispatchableJob>? LoosJobs { get; set; }
        public List<IDispatchableJob> AllJobs { get; internal set; }
    }
}
