using static Dispatcher;

public interface IDispatchableJob
{
    public string JobId { get; }
    Task Dispatch(DispatchableJobArgument argument);
}