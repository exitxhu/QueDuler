public interface IBroker
{
    public event Func<object, OnMessageReceivedArgs, Task> OnMessageReceived;

    void PushMessage(OnMessageReceivedArgs message);
    Task StartConsumingAsyn(CancellationToken cancellationToken);
}
public record OnMessageReceivedArgs(string Message, string JobPath = null);

