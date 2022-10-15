public interface IBroker
{
    public event EventHandler<OnMessageReceivedArgs> OnMessageReceived;

    void PushMessage(OnMessageReceivedArgs message);
    Task StartConsumingAsyn(CancellationToken cancellationToken);
}
public record OnMessageReceivedArgs(string Message, string JobPath = null);

