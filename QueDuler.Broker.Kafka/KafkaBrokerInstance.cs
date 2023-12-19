using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace QueDuler.Helpers;

public abstract class KafkaBrokerInstance
{
    public required ConsumerConfig BrokerConfig { get; set; }
    public required IEnumerable<TopicMetadata> PathConfigs { get; set; }
    public ServiceLifetime BrokerLifetime { get; set; } = ServiceLifetime.Transient;

}
