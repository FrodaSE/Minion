# Minion

Minion is a modern, testable background job scheduler for .NET applications.
Minion will handle running your background job in a reliable way with SQL Server backed storage.

Example of scenarios when you can use Minion:

* Fire web hooks
* Database cleanup
* Creating recurring automated reports
* Batch sending emails
* ... and more

## Installation

Minion is available as a NuGet package. You can install it using the NuGet Package Console windows in Visual Studio:

```
PM> Install-Package Froda.Minion
```

## Usage

To run the server, add the folliwing lines of code:

```
var dateService = new UtcDateService();
var store = new SqlStorage(dateService, '<connection string>');

using (var engine = new BatchEngine(store))
{
    Console.WriteLine("Starting ...");

    engine.Start();

    Console.ReadKey();
}
```

All jobs need to inherit from `Job` or `Job<TInput>`:

```
public class SimpleJob : Job
{
    public override async Task<JobResult> ExecuteAsync()
    {
        Console.WriteLine("Hello from simple job");

        return Finished();
    }
}

public class JobWithInput : Job<JobWithInput.Input>
{
    public class Input
    {
        public string Text { get; set; }
    }

    public override async Task<JobResult> ExecuteAsync(Input input)
    {
        Console.WriteLine("Hello from job with input: " + input.Text);

        return Finished();
    }
}
```


To schedule jobs you first need an instance of JobScheduler:
```
var scheduler = new JobScheduler(store, dateService);
```

**Fire-and-forget**

The job will be executed as soon as possible.

```
await scheduler.QueueAsync<SimpleJob>();

var input = new JobWithInput.Input { Text = "This is awesome" };
await scheduler.QueueAsync<JobWithInput, JobWithInput.Input>(input);
```

**Scheduled job**

The job will execute at the given time.

```
var date = new Date(2019, 04, 20);

await scheduler.QueueAsync<SimpleJob>(date);

var input = new JobWithInput.Input { Text = "This is awesome" };
await scheduler.QueueAsync<JobWithInput, JobWithInput.Input>(input, date);
```

**Recurring jobs**

Recurring jobs are scheduled as normal jobs, but need to return a new DueTime:

```
public class RecurringJob : Job
{
    private readonly IDateService _dateService;

    public RecurringJob(IDateService dateService) {
        _dateService = dateService;
    }

    public override async Task<JobResult> ExecuteAsync()
    {
        Console.WriteLine("Hello from recurring job, I will execute every 2 seconds");

        return Reschedule(_dateService.GetNow().AddSeconds(2));
    }
}
```

**Sequence**

This will force the jobs to run in sequnce:

```
var sequence = new Sequence();

sequence.Add<FirstJob>(); //This will run first
sequence.Add<SecondJob>(); //When the FirstJob is finished this will run

await scheduler.QueueAsync(sequence);
```

**Set**

These jobs will run in parallel if possible:

```
var set = new Set();

set.Add<FirstJob>();
set.Add<SecondJob>();

await scheduler.QueueAsync(set);
```

**More advanced jobs**

You can run a set in a sequence, and a sequence in a set:

```
var sequence = new Sequence();

sequence.Add<FirstJob>(); //This will run first

var set = new Set();

//Second and third job will run in parallel if possible
set.Add<SecondJob>(); 
set.Add<ThirdJob>(); 

sequence.Add(set);

sequence.Add<FourthJob>(); //Only when both second and third job is finished, this will run

await scheduler.QueueAsync(sequence);
```

**Testing**

With the TestingBatchEngine you can simulate your jobs with the ability to time travel.

For an example, if you schedule a job to send an email two days from now, you can fast forward and the batch engine will act as if two days have passed.

```
[Fact]
public async Task Can_Send_Email() {

    //Setup
    ...

    var startDate = new DateTime(2018, 04, 20);
    var dateSimulationService = new SimpleDateSimulationService(startDate);
    var store = new InMemoryStorage(dateSimulationService);

    var eninge = new TestingBatchEngine(store, dateSimulationService);
    var scheduler = new JobScheduler(store, dateSimulationService)

    await scheduler.QueueAsync<SendEmailJob>(startDate.AddDays(2)); //Schedule job to two days from now

    await engine.AdvanceToDateAsync(startDate.AddDays(2)); //Fast forward to two days from now

    //Assert
    ...
}
```

**Dependency Injection**

To hook up your IoC container to, you need to create a class that inherits from IDependencyResolver:

```
public class CustomDependencyResolver : IDependencyResolver 
{
    private readonly IContainer _container;

    public CustomDependencyResolver(IContainer container) 
    {
        _container = container;
    }

    public bool Resolve(Type type, out object resolvedType)
    {
        resolvedType = _container.Resolve(type);

        if(resolvedType == null)
        {
            return false;    
        }

        return true;
    }
}
```

Then you need to pass your resolver to the batch engine:
```
...
var container = new Container();
var resolver = new CustomDependencyResolver(container);

using(var engine = new BatchEngine(store, resolver)) {

    engine.Start();

    Console.ReadKey();

}
```

## License

Minion goes under The MIT License (MIT).