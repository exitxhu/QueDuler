using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueDuler
{
    public class KafkaBroker : IBroker
    {
        private readonly string[] topics;

        public ConsumerConfig Config { get; }
        public event EventHandler<string> OnMessageReceived;

        public KafkaBroker(ConsumerConfig config, params string[] topics)
        {

            Config = config;
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
                    var consumeResult = consumer.Consume();

                    OnMessageReceived(this, consumeResult.Message.Value);
                }

                consumer.Close();
            }

        }
        public void PushMessage(string message) => OnMessageReceived?.Invoke(this, message);
    }
}
