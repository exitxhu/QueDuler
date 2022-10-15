using static Dispatcher;

public interface IDispatchableJob
{
    public string JobId { get; }
    public string JobPath { get; }
    Task Dispatch(DispatchableJobArgument argument);
}