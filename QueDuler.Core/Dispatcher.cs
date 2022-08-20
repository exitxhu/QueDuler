// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using QueDuler.Core;
using QueDuler.Helpers;

public partial class Dispatcher
{
    private readonly IBroker? _broker;
    private readonly IScheduler? _scheduler;
    private readonly DispatcherArg _jobs;
    private readonly IServiceProvider _provider;
    public Dispatcher(IBroker broker, IScheduler scheduler, DispatcherArg jobs, IServiceProvider provider)
    {
        _broker = broker;
        _scheduler = scheduler;
        _jobs = jobs;
        _provider = provider;
    }
    public Dispatcher(IScheduler scheduler, DispatcherArg jobs, IServiceProvider provider)
    {
        _broker = null;
        _scheduler = scheduler;
        _jobs = jobs;
        _provider = provider;
    }
    public Dispatcher(IBroker broker, DispatcherArg jobs, IServiceProvider provider)
    {
        _scheduler = null;
        _broker = broker;
        _jobs = jobs;
        _provider = provider;
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
            _broker.OnMessageReceived += (s, a) =>
            {
                try
                {
                    var check = DispatchableJobArgument.TryParse(a, out var arg);
                    if (check)
                    {
                        var j = _provider.CreateScope().ServiceProvider.GetService(
                            dispatchables.SingleOrDefault(n => n.JobId == arg.JobId)?.GetType()) as IDispatchableJob;
                        j.Dispatch(arg);

                    }
                }
                catch (Exception ex)
                {
                //Todo: something about it
                    Console.WriteLine(ex);
                }
            };
            Task.Run(() => _broker.StartConsumingAsyn(cancellationToken)).ConfigureAwait(false);
        }
    }
}
