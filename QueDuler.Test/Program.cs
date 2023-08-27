// See https://aka.ms/new-console-template for more information
using Hangfire.Common;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCrontab;
using QueDuler.Helpers;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

Console.WriteLine("Hello, World!");
var services = new ServiceCollection()
        .AddLogging();

services.AddQueduler(a => a.AddKafkaBroker(services, new Confluent.Kafka.ConsumerConfig
{
    BootstrapServers = "78.47.21.107:9092",
    GroupId = "aa5",
    AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest,

}, topics: "jtopic_test")
.AddJobAssemblies(typeof(Program))
.AddInMemoryScheduler(services, new()
{
    MaxInMemoryLogCount = 10,
    TickTimeMillisecond = 1000,
}));

var serviceProvider = services.BuildServiceProvider();

//configure console logging
serviceProvider.GetService<ILoggerFactory>();





var dispatcher = serviceProvider.GetService<Dispatcher>();
dispatcher.Start(new CancellationToken { });
int o = 0;


var transformBlock = new TransformBlock<int?, string>(async request =>
{
    var d = Random.Shared.Next(0, 5) * 1750;

    await Task.Delay(d);
    var response = request.ToString() + $", delay: {d}, ord: {o++}";
    return response;
}, new ExecutionDataflowBlockOptions
{
    MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
    EnsureOrdered = false,

});

var actionBlock = new ActionBlock<string>(response =>
{
    Console.WriteLine(string.IsNullOrEmpty(response) ? "Request failed" : $"Request was successful :{response}");
});

transformBlock.LinkTo(actionBlock, new DataflowLinkOptions
{
    PropagateCompletion = true
});

transformBlock.Post(1);
transformBlock.Post(2);
transformBlock.Post(3);
transformBlock.Post(4);
transformBlock.Post(5);
transformBlock.Post(6);

await transformBlock.Completion;

public class SampleIneMem : ISchedulableJob
{
    public SampleIneMem(Dispatcher dispatcher1)
    {
        Dispatcher1 = dispatcher1;
    }
    public string JobId => "ana";
    //public string Cron => "*/5 * * * *";
    public string Cron => "* * * * * *";

    public Dispatcher Dispatcher1 { get; }

    public async Task Do(params object[] arguments)
    {
        await Console.Out.WriteLineAsync("DODODO");
    }

    public TimeZoneInfo TimeZoneInfo() => System.TimeZoneInfo.Local;
}

public class SampleJOb : IDispatchableJob
{
    public string JobId => "SyncRedisWithDbJob";

    public string JobPath => "jtopic_test";

    public bool LoosArgument => true;

    public async Task Dispatch(DispatchableJobArgument argument, object? originalMessage = null)
    {
        await Task.Delay(2000);
    }
}
