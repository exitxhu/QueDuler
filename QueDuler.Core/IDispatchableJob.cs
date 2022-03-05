public interface IDispatchableJob
{
    public string JobId { get; }
    void Dispatch(params object[] arguments);
}