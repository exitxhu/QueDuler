using Confluent.Kafka;
using linqPlusPlus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QueDuler.Core.Internals;
using QueDuler.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace QueDuler;

public class KafkaBroker : IBroker
{
    private readonly ILogger<KafkaBroker> _logger;
    private readonly List<TopicMetadata> _topics;
    private IProducer<Null, string> _producer;
    private IProducer<string, string> _keyedProducer;
    private int consumerCount;
    private Dictionary<string, int> startedConsumerCount = new();
    private List<Task> startUpConsumers = new List<Task>();
    private List<Task> runtimeConsumers = new List<Task>();
    public ConsumerConfig Config { get; }
    public string Key { get => Config.BootstrapServers; }
    TimeSpan timeout = default;

    public event Func<object, OnMessageReceivedArgs, Task> OnMessageReceived;

    public KafkaBroker(ConsumerConfig config,
        ILogger<KafkaBroker> logger,
        string[] topics,
        IServiceProvider serviceProvider
        )
    {
        Config = config;
        _logger = logger;
        _topics = topics.Select(a => new TopicMetadata
        {
            TopicName = a
        }).ToList();
        InitiateProducer(serviceProvider);
        consumerCount = topics.Length;
        timeout = TimeSpan.FromMilliseconds(Config.MaxPollIntervalMs.HasValue ? Config.MaxPollIntervalMs.Value - (0.05 * Config.MaxPollIntervalMs.Value) : 300000);
    }
    public KafkaBroker(ConsumerConfig config,
        ILogger<KafkaBroker> logger,
        List<TopicMetadata> topics,
        IServiceProvider serviceProvider
        )
    {
        Config = config;
        _logger = logger;
        _topics = topics;
        InitiateProducer(serviceProvider);
        consumerCount = topics.Count;

    }
    private void InitiateProducer(IServiceProvider serviceProvider)
    {
        var conf = serviceProvider.GetKeyedService<ProducerConfig>(Key);
        _producer = new ProducerBuilder<Null, string>(conf).Build();
        _keyedProducer = new ProducerBuilder<string, string>(conf).Build();
    }
    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        foreach (var topic in _topics)
        {
            for (startedConsumerCount[topic.TopicName] = startedConsumerCount.GetValueOrDefault(topic.TopicName); startedConsumerCount[topic.TopicName] < topic.ConsumerCount; startedConsumerCount[topic.TopicName]++)
            {
                startUpConsumers.Add(AddConsumer(timeout, topic.TopicName, cancellationToken));
            }
        }
        await Task.WhenAll(startUpConsumers);
    }
    public async Task AddRuntimeConsumer(TimeSpan timeout , string topicName, CancellationToken cancellationToken)
    {
        startedConsumerCount[topicName] = startedConsumerCount.GetValueOrDefault(topicName) + 1;
        runtimeConsumers.Add(AddConsumer(timeout, topicName, cancellationToken));
        Task.WhenAll(runtimeConsumers);
    }
    public async Task AddRuntimeConsumer(string topicName, CancellationToken cancellationToken)
    {
        startedConsumerCount[topicName] = startedConsumerCount.GetValueOrDefault(topicName) + 1;
        runtimeConsumers.Add(AddConsumer(timeout, topicName, cancellationToken));
        Task.WhenAll(runtimeConsumers);
    }

    private Task AddConsumer(TimeSpan timeout, string topicName, CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            int cNumber = startedConsumerCount[topicName];
            string msg = string.Empty;
            string id = $"{topicName}_{cNumber + 1}";
            var sw = new Stopwatch();
            var consumer = new ConsumerBuilder<string, string>(Config).Build();
            try
            {
                _logger.LogWarning("Kafka consumer: will subscribe to: {0}, consumer number {1}", topicName, id);
                consumer.Subscribe(topicName);
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(timeout);
                        sw.Restart();
                        msg = consumeResult?.Message?.Value;
                        if (msg is null) continue;
                        _logger.LogDebug("Kafka broker has received a new message: {0}, consumer number {1}", msg, id);
                        var t = OnMessageReceived(this, new OnMessageReceivedArgs(msg, id, topicName, consumeResult.Message));
                        await t.WaitAsync(cancellationToken);
                    }
                    catch (Exception ex) when (ex.Message.Contains("Application maximum poll", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _logger.LogCritical(ex, "Kafka broker has encountered some error, message is: {0}, consumer number {1}", msg, id);
                        consumer = new ConsumerBuilder<string, string>(Config).Build();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Kafka broker has encountered some error, message is: {0}, consumer number {1}", msg, id);
                        break;
                    }
                    finally
                    {
                        _logger.LogInformation($"topic {topicName} no {cNumber} finished in {sw.ElapsedMilliseconds} ms");
                    }
                }
                consumer.Close();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Kafka broker on topic: {0}, consumer number {1} has some major error and can not start consuming!", topicName, cNumber + 1);
                throw;
            }
        });
    }

    public void MockPushMessage(OnMessageReceivedArgs message) => OnMessageReceived?.Invoke(this, message);
    public async Task PushMessage(OnMessageReceivedArgs message, string key = null, Dictionary<string, byte[]> headers = null)
    {
        var h = new Headers();
        foreach (var item in headers)
            h.Add(item.Key, item.Value);
        if (key.HasContent())
            await _keyedProducer.ProduceAsync(message.JobPath, new Message<string, string>()
            {
                Value = message.Message,
                Key = key,
                Headers = h
            });
        else
            await _producer.ProduceAsync(message.JobPath, new Message<Null, string>()
            {
                Value = message.Message,
                Headers = h
            });

    }
    public (string key, Dictionary<string, byte[]> headers) DeconstructMessage(object originalMessage)
    {
        if (originalMessage is Message<string, string> m)
            return (m.Key, m.Headers.Select(a => new KeyValuePair<string, byte[]>(a.Key, a.GetValueBytes())).ToDictionary());
        return default;
    }

}
