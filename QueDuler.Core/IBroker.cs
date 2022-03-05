public interface IBroker
{
    public event EventHandler<string> OnMessageReceived;

    void PushMessage(string message);
    Task StartConsumingAsyn(CancellationToken cancellationToken);
}
