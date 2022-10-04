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
        public event EventHandler<string> OnMessageReceived;

        public KafkaBroker(ConsumerConfig config, ILogger<KafkaBroker> logger, params string[] topics)
        {

            Config = config;
            _logger = logger;
            _topics = topics;
        }
        public async Task StartConsumingAsyn(CancellationToken cancellationToken)
        {
            //TODO: 1-ack? 2- failure tolerance?
            using (var consumer = new ConsumerBuilder<Ignore, string>(Config).Build())
            {
                try
                {
                    _logger.LogWarning("Kafka broker will subscrib to: {0}", String.Join(", ", _topics));
                    consumer.Subscribe(_topics);
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume();
                            _logger.LogWarning("Kafka broker has received a new message: {0}", consumeResult.Message.Value);
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
                catch (Exception ex)
                {
                    _logger.LogCritical(ex,"Kafka broker has some major error and con not start consuming!");
                    throw;
                }
            }
        }
        public void PushMessage(string message) => OnMessageReceived?.Invoke(this, message);
    }
}
