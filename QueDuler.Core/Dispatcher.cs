// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using QueDuler.Core;
using QueDuler.Helpers;

public partial class Dispatcher
{
    private readonly IBroker broker;
    private readonly IScheduler scheduler;
    private readonly DispatcherArg jobs;
    private readonly IServiceProvider provider;
    public Dispatcher(IBroker broker, IScheduler scheduler, DispatcherArg jobs, IServiceProvider provider)

    {
        this.broker = broker;
        this.scheduler = scheduler;
        this.jobs = jobs;
        this.provider = provider;
    }
    public void Start(CancellationToken cancellationToken)
    {
        HashSet<IDispatchableJob> dispatchables = new();
        HashSet<ISchedulableJob> schedules = new();
            // we need to resolve the jobs inorder to get the jobid from the implentation(in run time)
            // but we should to resolve them again when we want to run them, other wise it wont be thread safe!
            // specially for dispatchables
        foreach (var job in jobs.SchedulableJobs)
        {
            var j = provider.GetService(job) as ISchedulableJob
                ?? throw new JobNotInjectedException(job.FullName);
            scheduler.Schedule(j);
        }
        foreach (var job in jobs.DispatchableJobs)
        {
            var j = provider.GetService(job) as IDispatchableJob
                ?? throw new JobNotInjectedException(job.FullName);
            dispatchables.Add(j);
        }
        broker.OnMessageReceived += (s, a) =>
        {
            try
            {
                var check = DispatchableJobArgument.TryParse(a, out var arg);
                if (check)
                {
                    var j = provider.CreateScope().ServiceProvider.GetService(
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
        Task.Run(() => broker.StartConsumingAsyn(cancellationToken)).ConfigureAwait(false);
    }
}
