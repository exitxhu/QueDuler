// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QueDuler.Helpers;

Console.WriteLine("Hello, World!");
var services = new ServiceCollection()
        .AddLogging();

services.AddQueduler(a => a.AddKafkaBroker(services, new Confluent.Kafka.ConsumerConfig
{
    BootstrapServers = "78.47.21.107:9092",
    GroupId = "aa",
    AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest,

}, topics: "jtopic").AddJobAssemblies(typeof(Program)));

var serviceProvider = services.BuildServiceProvider();

//configure console logging
serviceProvider
    .GetService<ILoggerFactory>();

var dispatcher= serviceProvider.GetService<Dispatcher>();
dispatcher.Start(new CancellationToken { });
Console.ReadLine();

public class SampleJOb : IDispatchableJob
{
    public string JobId => "SyncRedisWithDbJob";

    public string JobPath => "jtopic";

    public async Task Dispatch(DispatchableJobArgument argument)
    {
        Thread.Sleep(20000);
    }
}