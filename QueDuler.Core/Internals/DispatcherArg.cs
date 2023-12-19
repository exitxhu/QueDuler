namespace QueDuler.Core.Internals;

public class DispatcherArg
{
    public IEnumerable<Type> DispatchableJobs { get; set; }
    public IEnumerable<Type> SchedulableJobs { get; set; }
    public IEnumerable<Type> ObservableJobs { get; set; }
}
