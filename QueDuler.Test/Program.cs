// See https://aka.ms/new-console-template for more information
using Hangfire.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCrontab;
using QueDuler.Helpers;
using System.Threading.Channels;

Console.WriteLine("Hello, World!");
var services = new ServiceCollection()
        .AddLogging();

services.AddQueduler(a => a.AddKafkaBroker(services, new Confluent.Kafka.ConsumerConfig
{
    BootstrapServers = "78.47.21.107:9092",
    GroupId = "aa",
    AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest,

}, topics: "jtopic_InsertOrderDraft").AddJobAssemblies(typeof(Program))
.AddInMemoryScheduler(services,new()
{
    MaxInMemoryLogCount = 10,
    TickTimeMillisecond = 1000,
}));

var serviceProvider = services.BuildServiceProvider();

//configure console logging
serviceProvider
    .GetService<ILoggerFactory>();

var dispatcher = serviceProvider.GetService<Dispatcher>();
dispatcher.Start(new CancellationToken { });


Task.Delay(100000000).Wait();

public class sampleInemem : ISchedulableJob
{
    public string JobId => "ana";
    //public string Cron => "*/5 * * * *";
    public string Cron => "* * * * * *";

    public async Task Do(params object[] arguments)
    {
        await Console.Out.WriteLineAsync("DODODO");
    }

    public TimeZoneInfo TimeZoneInfo()=> System.TimeZoneInfo.Local;
}

public class SampleJOb : IDispatchableJob
{
    public string JobId => "SyncRedisWithDbJob";

    public string JobPath => "jtopic_InsertOrderDraft";

    public bool LoosArgument => true;

    public async Task Dispatch(DispatchableJobArgument argument, object? originalMessage = null)
    {
        await Task.Delay(2000);
    }
}