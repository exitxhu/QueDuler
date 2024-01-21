using Microsoft.Extensions.Logging;
using QueDuler.Core.Internals;
using QueDuler.Scheduler.InMemory;

namespace QueDuler;

public class InMemoryBroker : IBroker
{
    private readonly ILogger<InMemoryBroker> _logger;
    private readonly string[] _topics;

    public BrokerConfig Config { get; }

    public string Key => "SomeKey" + Config.GetHashCode();

    public event Func<object, OnMessageReceivedArgs, Task> OnMessageReceived;

    public InMemoryBroker(BrokerConfig config, ILogger<InMemoryBroker> logger, params string[] topics)
    {
        Config = config;
        _logger = logger;
        _topics = topics;
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        List<Task> tasks = new List<Task>();
        foreach (var topic in _topics)
        {
            tasks.Add(Task.Run(async () =>
            {
                string msg = string.Empty;
                try
                {
                    _logger.LogWarning("InMemory consumer: will subscrib to: {0}", topic);
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            var t = OnMessageReceived(this, new OnMessageReceivedArgs(msg, topic, ""));
                            await t.WaitAsync(cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogCritical(ex, "InMemory broker has encountered some error, messgae is: {0}", msg);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "InMemory broker on topic: {0} has some major error and con not start consuming!", topic);
                    throw;
                }
            }));
        }
        await Task.WhenAll(tasks);
    }
    public void MockPushMessage(OnMessageReceivedArgs message) => OnMessageReceived?.Invoke(this, message);
    public Task PushMessage(OnMessageReceivedArgs message, string key = null, Dictionary<string, byte[]> headers = null)
    {
        throw new NotImplementedException();
    }

    public (string key, Dictionary<string, byte[]> headers) DeconstructMessage(object originalMessage)
    {
        throw new NotImplementedException();
    }
}
