using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QueDuler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QueDuler.Helpers;

public static class DiExtension
{
    public static QuedulerOptions AddKafkaBroker(this QuedulerOptions configuration,
                                                 IServiceCollection services,
                                                 ConsumerConfig KafkaConfig,
                                                 ServiceLifetime BrokerLifetime = ServiceLifetime.Transient,
                                                 params string[] topics)
    {
        services.Add(new ServiceDescriptor(typeof(IBroker), (s) =>
           new KafkaBroker(KafkaConfig, s.GetRequiredService<ILogger<KafkaBroker>>(), topics), BrokerLifetime));
        return configuration;
    }
    public static QuedulerOptions AddKafkaBroker(this QuedulerOptions configuration,
                                         IServiceCollection services,
                                         ConsumerConfig KafkaConfig,
                                         List<TopicMetadata> topics,
                                         ServiceLifetime BrokerLifetime = ServiceLifetime.Transient)
    {
        services.Add(new ServiceDescriptor(typeof(IBroker), (s) =>
           new KafkaBroker(KafkaConfig, s.GetRequiredService<ILogger<KafkaBroker>>(), topics), BrokerLifetime));
        return configuration;
    }
    public static QuedulerOptions AddTypedKafkaBroker<T>(this QuedulerOptions configuration,
                                        T brokerConfig,
                                        IServiceCollection services
                                        ) where T : BrokerInstance<ConsumerConfig, TopicMetadata>
    {
        services.Add(new ServiceDescriptor(typeof(Broker<T>), (s) =>
           new KafkaBroker(brokerConfig.BrokerConfig,
                        s.GetRequiredService<ILogger<KafkaBroker>>(),
                        brokerConfig.PathConfigs.ToList()
                        ), brokerConfig.BrokerLifetime));
        return configuration;
    }
}
public abstract class KafkaBrokerInstance : BrokerInstance<ConsumerConfig, TopicMetadata>
{
    public override required ConsumerConfig BrokerConfig { get; set; }
    public override required IEnumerable<TopicMetadata> PathConfigs { get; set; }
    public override ServiceLifetime BrokerLifetime { get; set; } = ServiceLifetime.Transient;

}
