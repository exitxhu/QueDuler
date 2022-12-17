using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

                tasks.Add(Task.Run(async () =>
                  {
                      string msg = string.Empty;
                      using var consumer = new ConsumerBuilder<Ignore, string>(Config).Build();
                      try
                      {
                          _logger.LogWarning("Kafka consumer: will subscrib to: {0}, consumer number {1}", topic.TopicName, i + 1);
                          consumer.Subscribe(topic.TopicName);
                          while (!cancellationToken.IsCancellationRequested)
                          {
                              try
                              {
                                  var consumeResult = consumer.Consume();
                                  msg = consumeResult.Message.Value;
                                  _logger.LogDebug("Kafka broker has received a new message: {0}, consumer number {1}", msg, i + 1);
                                  await OnMessageReceived(this, new OnMessageReceivedArgs(msg, topic.TopicName));
                              }
                              catch (Exception ex) when (ex.Message.Contains("Application maximum poll", StringComparison.InvariantCultureIgnoreCase))
                              {
                                  _logger.LogCritical(ex, "Kafka broker has encountered some error, messgae is: {0}, consumer number {1}", msg, i + 1);
                              }
                              catch (Exception ex)
                              {
                                  _logger.LogCritical(ex, "Kafka broker has encountered some error, messgae is: {0}, consumer number {1}", msg, i + 1);
                                  break;
                              }
                          }
                          consumer.Close();
                      }
                      catch (Exception ex)
                      {
                          _logger.LogCritical(ex, "Kafka broker on topic: {0}, consumer number {1} has some major error and con not start consuming!", topic, i + 1);
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