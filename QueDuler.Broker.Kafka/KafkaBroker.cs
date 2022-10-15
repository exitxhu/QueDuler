using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueDuler
{
    public class KafkaBroker : IBroker
    {
        private readonly ILogger<KafkaBroker> _logger;
        private readonly string[] _topics;

        public ConsumerConfig Config { get; }
        public event EventHandler<OnMessageReceivedArgs> OnMessageReceived;

        public KafkaBroker(ConsumerConfig config, ILogger<KafkaBroker> logger, params string[] topics)
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
                tasks.Add(Task.Run(() =>
                  {
                      string msg = string.Empty;
                      using var consumer = new ConsumerBuilder<Ignore, string>(Config).Build();
                      try
                      {
                          _logger.LogWarning("Kafka consumer: {0} will subscrib to: {1}", topic, consumer.MemberId);
                          consumer.Subscribe(topic);
                          while (!cancellationToken.IsCancellationRequested)
                          {
                              try
                              {
                                  var consumeResult = consumer.Consume();
                                  msg = consumeResult.Message.Value;
                                  _logger.LogInformation("Kafka broker has received a new message: {0}", msg);
                                  OnMessageReceived(this, new OnMessageReceivedArgs(msg, topic));
                              }
                              catch (Exception ex) when (ex.Message.Contains("Application maximum poll", StringComparison.InvariantCultureIgnoreCase))
                              {
                                  _logger.LogCritical(ex, "Kafka broker has encountered some error, messgae is: {0}", msg);
                              }
                              catch (Exception ex)
                              {
                                  _logger.LogCritical(ex, "Kafka broker has encountered some error, messgae is: {0}", msg);
                                  break;
                              }
                          }
                          consumer.Close();
                      }
                      catch (Exception ex)
                      {
                          _logger.LogCritical(ex, "Kafka broker on topic: {0} has some major error and con not start consuming!", topic);
                          throw;
                      }
                  }));
            }
            await Task.WhenAll(tasks);
        }
        public void PushMessage(OnMessageReceivedArgs message) => OnMessageReceived?.Invoke(this, message);
    }
}
