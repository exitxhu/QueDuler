using static Dispatcher;

public interface IDispatchableJob
{
    public string JobId { get; }
    public string JobPath { get; }
    /// <summary>
    /// if u select this, the original message will be passed down to Dispatch() even if the argument is null(incase its unparsable, unvalid etc.)
    /// and this job will be trigered for every msg in this path(no job id verification)
    /// </summary>
    public bool LoosArgument { get; }

    Task Dispatch(DispatchableJobArgument argument, object? originalMessage = null);
}
