﻿// See https://aka.ms/new-console-template for more information
using Confluent.Kafka;
using Hangfire.Common;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCrontab;
using QueDuler;
using QueDuler.Core.Internals;
using QueDuler.Helpers;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

Console.WriteLine("Hello, World!");
var services = new ServiceCollection()
        .AddLogging();
services.AddQueduler(a => a.AddJobAssemblies(typeof(Program)));

services.AddQueduler(a => a.AddKafkaBroker(new AffilKaf
{
    BrokerConfig = new Confluent.Kafka.ConsumerConfig
    {
        BootstrapServers = "78.47.21.107:9092",
        GroupId = "aa5",
        AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest,
    },
    PathConfigs = new List<QueDuler.TopicMetadata> { new() { TopicName = "jtopic_CalculateOrderEvents", ConsumerCount = 1 }
}
}, services).AddKafkaBroker(new AffilKaf
{
    BrokerConfig = new Confluent.Kafka.ConsumerConfig
    {
        BootstrapServers = "65.108.94.76:9092,65.21.241.239:9092,65.108.52.252:9092",
        GroupId = "jober_dev",
        AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest,
    },
    PathConfigs = new List<QueDuler.TopicMetadata> { new() { TopicName = "yadol_RTB_Call_Report", ConsumerCount = 1 }
}
}, services)
.AddJobAssemblies(typeof(Program))
.AddInMemoryScheduler(services, new()
{
    MaxInMemoryLogCount = 10,
    TickTimeMillisecond = 1000,
}));
var serviceProvider = services.BuildServiceProvider();

//configure console logging
serviceProvider.GetService<ILoggerFactory>();

var brokrts = serviceProvider.GetKeyedService<IBroker>("78.47.21.107:9092");
var timeout = TimeSpan.FromMilliseconds(300000);


var dispatcher = serviceProvider.GetService<Dispatcher>();
dispatcher.Start(new CancellationToken { });
int o = 0;
brokrts.AddRuntimeConsumer(timeout, "jtopic_CalculateOrderEvents", CancellationToken.None);


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
    public SampleIneMem(Dispatcher dispatcher1, JobResolver jobResolver)
    {
        Dispatcher1 = dispatcher1;
        JobResolver = jobResolver;
    }
    public string JobId => "ana";
    //public string Cron => "*/5 * * * *";
    public string Cron => "*/15 * * * * *";

    public Dispatcher Dispatcher1 { get; }
    public JobResolver JobResolver { get; }

    public async Task Do(params object[] arguments)

    {
        await Console.Out.WriteLineAsync("DODODO");
        var t = JobResolver.GetDispatchable("jtopic_test", "SyncRedisWithDbJob");
    }

    public TimeZoneInfo TimeZoneInfo() => System.TimeZoneInfo.Local;
}
public class SampleObsJOb : IObservableJob
{
    public string JobPath => "jtopic_test";

    int a = 0;
    Guid b = Guid.NewGuid();

    public async Task OnNext(object? originalMessage = null)
    {
        a++;
        throw new NotImplementedException();
    }

    public Task OnError(Exception ex)
    {
        throw new NotImplementedException();
    }

    public Task OnComplete()
    {
        throw new NotImplementedException();
    }
}
public class SampleJOb : IDispatchableJob, IRetriable
{
    public string JobId => "SyncRedisWithDbJob";

    public string JobPath => "jtopic_CalculateOrderEvents";
    public SampleJOb()
    {
        RetryPolicyPrototype = new RetryManager(this).SetALAPCount(3); ;
    }
    public bool LoosArgument => true;
    private RetryManager _retryPolicyBuilder;
    public RetryManager RetryPolicyPrototype
    {
        get => _retryPolicyBuilder;
        set => _retryPolicyBuilder = value;
    }

    public async Task Dispatch(DispatchableJobArgument argument, object? originalMessage = null)
    {
        if (originalMessage is Message<Ignore, string> msg)
        {

        }
        await Task.Delay(300);

        throw new NotImplementedException();
    }
}


public class AffilKaf : KafkaBrokerInstance
{
}