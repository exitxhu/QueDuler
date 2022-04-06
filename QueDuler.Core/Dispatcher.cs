// See https://aka.ms/new-console-template for more information
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
        foreach (var job in jobs.SchedulableJobs)
        {
            var j = provider.GetService(job) as ISchedulableJob
                ?? throw new NullReferenceException($"job FullName: {job.FullName} is not injected! be sure that you used the method \"AddQueduler()\" to inject the job assembly into di container, and also chosen the right assemblies");
            scheduler.Schedule(j);
        }
        foreach (var job in jobs.DispatchableJobs)
        {
            var j = provider.GetService(job) as IDispatchableJob
                ?? throw new NullReferenceException($"job FullName: {job.FullName} is not injected! be sure that you used the method \"AddQueduler()\" to inject the job assembly into di container, and also chosen the right assemblies");
            dispatchables.Add(j);
        }
        broker.OnMessageReceived += (s, a) =>
        {
            try
            {
                var check = DispatchableJobArgument.TryParse(a, out var arg);
                if (check)
                    dispatchables.SingleOrDefault(n => n.JobId == arg.JobId)?.Dispatch(arg);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        };
        Task.Run(() => broker.StartConsumingAsyn(cancellationToken)).ConfigureAwait(false);
    }
}
