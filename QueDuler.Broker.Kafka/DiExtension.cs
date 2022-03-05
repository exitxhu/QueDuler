using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using QueDuler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KafkaDuler.Helpers
{
    public static class DiExtension
    {

        public static QuedulerOptions AddKafkaBroker(this QuedulerOptions configuration, IServiceCollection services, ConsumerConfig KafkaConfig, ServiceLifetime BrokerLifetime = ServiceLifetime.Transient )
        {
            services.Add(new ServiceDescriptor(typeof(IBroker),(s) => new KafkaBroker(KafkaConfig), BrokerLifetime));
            return configuration;
        }
    }
}
