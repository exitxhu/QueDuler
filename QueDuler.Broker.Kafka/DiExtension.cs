using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QueDuler;
using QueDuler.Core.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
           new KafkaBroker(KafkaConfig,
           s.GetRequiredService<ILogger<KafkaBroker>>(), topics, s), BrokerLifetime));
        services.AddKeyedSingleton(KafkaConfig.BootstrapServers,
            (n, o) => new ProducerConfig
            {
                BootstrapServers = o as string,
                ClientId = Dns.GetHostName(),
            });
        return configuration;
    }
    public static QuedulerOptions AddKafkaBroker(this QuedulerOptions configuration,
                                         IServiceCollection services,
                                         ConsumerConfig KafkaConfig,
                                         List<TopicMetadata> topics,
                                         ServiceLifetime BrokerLifetime = ServiceLifetime.Transient)
    {
        services.Add(new ServiceDescriptor(typeof(IBroker), (s) =>
           new KafkaBroker(KafkaConfig, s.GetRequiredService<ILogger<KafkaBroker>>(), topics, s), BrokerLifetime));
        services.AddKeyedSingleton(KafkaConfig.BootstrapServers,
            (n, o) => new ProducerConfig
            {
                BootstrapServers = o as string,
                ClientId = Dns.GetHostName(),
            });
        return configuration;
    }
    public static QuedulerOptions AddKafkaBroker(this QuedulerOptions configuration,
                                        KafkaBrokerInstance brokerConfig,
                                        IServiceCollection services
                                        )
    {
        services.Add(new ServiceDescriptor(typeof(IBroker), (s) =>
           new KafkaBroker(brokerConfig.BrokerConfig,
                        s.GetRequiredService<ILogger<KafkaBroker>>(),
                        brokerConfig.PathConfigs.ToList(), s
                        ), brokerConfig.BrokerLifetime));
        services.AddKeyedSingleton(brokerConfig.BrokerConfig.BootstrapServers,
            (n, o) => new ProducerConfig
            {
                BootstrapServers = o as string,
                ClientId = Dns.GetHostName(),
            });
        return configuration;
    }
}
