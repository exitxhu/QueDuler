# QueDuler

Queduler help you create, schedule and dispatch your jobs easily and fast.

## How to use

* you can use the extension method: AddQueduler() in name space: KafkaDuler.Helpers to register the dispatcher 
* while configuring provide the assemblies which contains the jobs which implimented IDispatchableJob and ISchedulableJob interfaces
* if you decide to implement interface
### Dispatching Concept

1- you can dispatch jobs and trigger them with minimal effort, you only need to implement IBroker, or use some pre-implemented brokers like Queduler.Broker.Kafka
2- every job should imple IDispatchableJob, have a unique jobid(the class name maybe), and DIspatch method.
   > every job will registered in DiContainer so you caninject services in job constructor

3- When dispatcher started, it appends a OnMessageReceivedEventHandler to IBroker
4- Any time the message is equal to any jobid, the job will triggered instantly

### Scheduling Concept

- Same as IBroker, you can implement IScheduler or use pre-impl QueDuler.Scheduler.Hangfire
- every job should imple ISchedulableJob, have a unique jobid(the class name maybe), Cron expression and Do method.
   > every job will registered in DiContainer so you caninject services in job constructor
- When dispatcher starts, it schedule any job with Schedule methodin IScheduler interface using the cron expression.
- no more steps


```
contact me [exitchu@gmail.com]
```

