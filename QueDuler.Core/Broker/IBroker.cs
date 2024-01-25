namespace QueDuler.Core.Internals;

public interface IBroker
{
    public event Func<object, OnMessageReceivedArgs, Task> OnMessageReceived;
    public string Key { get; }
    void MockPushMessage(OnMessageReceivedArgs message);
    Task PushMessage(OnMessageReceivedArgs message, string key =null, Dictionary<string, byte[]> headers = null);
    Task StartConsumingAsync(CancellationToken cancellationToken);
    (string key, Dictionary<string, byte[]> headers) DeconstructMessage(object originalMessage);
    Task AddRuntimeConsumer(TimeSpan timeout, string topicName, CancellationToken cancellationToken);
    Task AddRuntimeConsumer(string topicName, CancellationToken cancellationToken);
}
