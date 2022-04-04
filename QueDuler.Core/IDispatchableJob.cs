public interface IDispatchableJob
{
    public string JobId { get; }
    Task Dispatch(params object[] arguments);
}