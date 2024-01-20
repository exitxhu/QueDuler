namespace QueDuler.Core.Internals;

public interface IBroker
{
    public event Func<object, OnMessageReceivedArgs, Task> OnMessageReceived;
    public string Key { get; }
    void MockPushMessage(OnMessageReceivedArgs message);
    Task PushMessage(OnMessageReceivedArgs message);
    Task StartConsumingAsync(CancellationToken cancellationToken);
}
