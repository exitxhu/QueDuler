# QueDuler

Queduler help you create, schedule and dispatch your jobs easily and fast.
It partially implement Actor pattern but the focus is to manage Async jobs fast, EZ and light weight.

## How to use

* you can use the extension method: AddQueduler() in name space: QueDuler.Helpers to register the dispatcher 
 ```C#
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
 ```
* you need to start the dispatcher so queduler starts it job! likely in startup after the build.
```C#
var dispatcher = serviceProvider.GetService<Dispatcher>();
dispatcher.Start(new CancellationToken { });
```
* while configuring provide the assemblies which contains the jobs which implimented IDispatchableJob and ISchedulableJob interfaces

### Dispatching Concept
****
#### Invoking a Job(Act):
1-  you need to sen a message to some well know path in your app, for example this code produce a message to a kafka topic:
```C#
var t = new ExtraData{
ID=1,
Name = "someName"
};
await _broker.ProduceAsync(new DispatchableJobArgument("SyncRedisWithDbJob", t).ToJson(), "jtopic_InsertOrderDraft", false);
```
2- DispatchableJobArgument is standard job argument, which should be provided it as the message body (if job is loosArgument it is not neccessary, we will get to it).
```C#
DispatchableJobArgument(string jobId, object argumentObject = null,
 bool isBroadCast = false)
```
argobj could be any object ou want to have in msg and IsBroodcast if set to true will trigger all the jobs at the end of the path (JobId will not match).

#### Receiving the job Invokation req

1- you can dispatch jobs and trigger them with minimal effort, you only need to implement IBroker, or use some pre-implemented brokers like Queduler.Broker.Kafka.

2- every job should imple IDispatchableJob, have a unique jobid(the class name maybe), and DIspatch method.
   > every job will registered in DiContainer so you caninject services in job constructor

3- When dispatcher started, it appends a OnMessageReceivedEventHandler to IBroker.

4- Any time the message is equal to any jobid, the job will triggered instantly.
```C#

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
```
Every time a received message meet the criteria, the Dispatch method will be invoked.

1- JobId is unique for every job.

2- JobPath is an address that should be known, it could be a topic in kafka or a exchange in RQ.

3- LoosArgument: if true every message at the JobPath will be used as originalMessage parameter and u should cast it accordingly, if false then the message should be parsable to DispatchableJobArgument and then the JobId matches.
### Scheduling Concept

- Same as IBroker, you can implement IScheduler or use pre-impl QueDuler.Scheduler.Hangfire
- every job should imple ISchedulableJob, have a unique jobid(the class name maybe), Cron expression and Do method.
   > every job will registered in DiContainer so you caninject services in job constructor
- When dispatcher starts, it schedule any job with Schedule methodin IScheduler interface using the cron expression.
- consider this job implementation:
- no more steps

#### if you decide to implement funtionalities yourself:

implement and inject these Interfaces:
```C#
public interface IScheduler
{
    public Task Schedule(ISchedulableJob job);
}

public interface IBroker
{
    public event Func<object, OnMessageReceivedArgs, Task> OnMessageReceived;

    void PushMessage(OnMessageReceivedArgs message);
    Task StartConsumingAsyn(CancellationToken cancellationToken);
}
```




```
contact me [exitchu@gmail.com]
```
