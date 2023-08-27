using Microsoft.Extensions.Logging;
using QueDuler.Scheduler.InMemory;

namespace QueDuler;

public class InMemoryBroker : IBroker
{
    private readonly ILogger<InMemoryBroker> _logger;
    private readonly string[] _topics;

    public BrokerConfig Config { get; }
    public event Func<object, OnMessageReceivedArgs, Task> OnMessageReceived;

    public InMemoryBroker(BrokerConfig config, ILogger<InMemoryBroker> logger, params string[] topics)
    {
        Config = config;
        _logger = logger;
        _topics = topics;
    }
  
    public async Task StartConsumingAsyn(CancellationToken cancellationToken)
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
                                await OnMessageReceived(this, new OnMessageReceivedArgs(msg, topic, ""));
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
    public void PushMessage(OnMessageReceivedArgs message) => OnMessageReceived?.Invoke(this, message);
}
