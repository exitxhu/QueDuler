using Confluent.Kafka;
using Microsoft.Extensions.Logging;
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

    public ConsumerConfig Config { get; }
    public event Func<object, OnMessageReceivedArgs, Task> OnMessageReceived;

    public KafkaBroker(ConsumerConfig config, ILogger<KafkaBroker> logger, params string[] topics)
    {
        Config = config;
        _logger = logger;
        _topics = topics.Select(a => new TopicMetadata
        {
            TopicName = a
        }).ToList();
    }
    public KafkaBroker(ConsumerConfig config, ILogger<KafkaBroker> logger, List<TopicMetadata> topics)
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
                                  var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(Config.MaxPollIntervalMs - (0.05 * Config.MaxPollIntervalMs) ?? 300000));
                                  sw.Restart();
                                  msg = consumeResult?.Message?.Value;
                                  if (msg is null) continue;
                                  _logger.LogDebug("Kafka broker has received a new message: {0}, consumer number {1}", msg, id);
                                  var t = OnMessageReceived(this, new OnMessageReceivedArgs(msg, id, topic.TopicName, consumeResult.Message));
                                  await t.WaitAsync(cancellationToken);
                              }
                              catch (Exception ex) when (ex.Message.Contains("Application maximum poll", StringComparison.InvariantCultureIgnoreCase))
                              {
                                  _logger.LogCritical(ex, "Kafka broker has encountered some error, messgae is: {0}, consumer number {1}", msg, id);
                                  consumer = new ConsumerBuilder<Ignore, string>(Config).Build();
                              }
                              catch (Exception ex)
                              {
                                  _logger.LogCritical(ex, "Kafka broker has encountered some error, messgae is: {0}, consumer number {1}", msg, id);
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
                          _logger.LogCritical(ex, "Kafka broker on topic: {0}, consumer number {1} has some major error and con not start consuming!", topic, consumerCount);
                          throw;
                      }
                  }));
            }
        }
        await Task.WhenAll(tasks);
    }
    public void PushMessage(OnMessageReceivedArgs message) => OnMessageReceived?.Invoke(this, message);
}
public class TopicMetadata
{
    public string TopicName { get; set; }
    public int ConsumerCount { get; set; } = 1;
}