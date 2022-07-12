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
        private readonly string[] topics;

        public ConsumerConfig Config { get; }
        public event EventHandler<string> OnMessageReceived;

        public KafkaBroker(ConsumerConfig config,ILogger<KafkaBroker> logger, params string[] topics)
        {

            Config = config;
            this._logger = logger;
            this.topics = topics;
        }
        public async Task StartConsumingAsyn(CancellationToken cancellationToken)
        {
            //TODO: 1-ack? 2- failure tolerance?
            using (var consumer = new ConsumerBuilder<Ignore, string>(Config).Build())
            {
                consumer.Subscribe(topics);

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume();

                        OnMessageReceived(this, consumeResult.Message.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Kafka broker has encountered some error");
                        throw;
                    }
                }

                consumer.Close();
            }

        }
        public void PushMessage(string message) => OnMessageReceived?.Invoke(this, message);
    }
}
