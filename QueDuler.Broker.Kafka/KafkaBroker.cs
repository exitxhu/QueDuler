using Confluent.Kafka;
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

    public ConsumerConfig Config { get; }
    public string Key { get => Config.BootstrapServers; }

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
    }
    private void InitiateProducer(IServiceProvider serviceProvider)
    {
        var conf = serviceProvider.GetKeyedService<ProducerConfig>(Key);
        _producer = new ProducerBuilder<Null, string>(conf).Build();
    }
    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        List<Task> tasks = new List<Task>();
        var timeout = TimeSpan.FromMilliseconds(Config.MaxPollIntervalMs.HasValue ? Config.MaxPollIntervalMs.Value - (0.05 * Config.MaxPollIntervalMs.Value) : 300000);
        foreach (var topic in _topics)
        {
            for (int i = 0; i < topic.ConsumerCount; i++)
            {
                var consumerCount = i + 1;
                tasks.Add(Task.Run(async () =>
                  {
                      string msg = string.Empty;
                      string id = $"{topic.TopicName}_{consumerCount}";
                      var sw = new Stopwatch();
                      var consumer = new ConsumerBuilder<Ignore, string>(Config).Build();
                      try
                      {
                          _logger.LogWarning("Kafka consumer: will subscrib to: {0}, consumer number {1}", topic.TopicName, id);
                          consumer.Subscribe(topic.TopicName);
                          while (!cancellationToken.IsCancellationRequested)
                          {
                              try
                              {
                                  var consumeResult = consumer.Consume(timeout);
                                  sw.Restart();
                                  msg = consumeResult?.Message?.Value;
                                  if (msg is null) continue;
                                  _logger.LogDebug("Kafka broker has received a new message: {0}, consumer number {1}", msg, id);
                                  var t = OnMessageReceived(this, new OnMessageReceivedArgs(msg, id, topic.TopicName, consumeResult.Message));
                                  await t.WaitAsync(cancellationToken);
                              }
                              catch (Exception ex) when (ex.Message.Contains("Application maximum poll", StringComparison.InvariantCultureIgnoreCase))
                              {
                                  _logger.LogCritical(ex, "Kafka broker has encountered some error, message is: {0}, consumer number {1}", msg, id);
                                  consumer = new ConsumerBuilder<Ignore, string>(Config).Build();
                              }
                              catch (Exception ex)
                              {
                                  _logger.LogCritical(ex, "Kafka broker has encountered some error, message is: {0}, consumer number {1}", msg, id);
                                  break;
                              }
                              finally
                              {
                                  _logger.LogInformation($"topic {topic} no {i} finished in {sw.ElapsedMilliseconds} ms");
                              }
                          }
                          consumer.Close();
                      }
                      catch (Exception ex)
                      {
                          _logger.LogCritical(ex, "Kafka broker on topic: {0}, consumer number {1} has some major error and can not start consuming!", topic, consumerCount);
                          throw;
                      }
                  }));
            }
        }
        await Task.WhenAll(tasks);
    }
    public void MockPushMessage(OnMessageReceivedArgs message) => OnMessageReceived?.Invoke(this, message);
    public async Task PushMessage(OnMessageReceivedArgs message)
    {
        await _producer.ProduceAsync(message.JobPath, new Message<Null, string>()
        {
            Value = message.Message,
        });
    }
}
