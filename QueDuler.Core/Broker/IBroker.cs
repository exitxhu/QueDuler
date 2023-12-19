namespace QueDuler.Core.Internals;

public interface IBroker
{
    public event Func<object, OnMessageReceivedArgs, Task> OnMessageReceived;

    void PushMessage(OnMessageReceivedArgs message);
    Task StartConsumingAsyn(CancellationToken cancellationToken);
}
