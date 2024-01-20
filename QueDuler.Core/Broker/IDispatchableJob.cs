using System;
using System.Text.Json;
using static Dispatcher;
using static RetryPolicy;

public interface IDispatchableJob
{
    public string JobId { get; }
    public string JobPath { get; }
    /// <summary>
    /// if u select this, the original message will be passed down to Dispatch() even if the argument is null(incase its un-parsable, invalid etc.)
    /// and this job will be triggered for every msg in this path(no job id verification)
    /// </summary>
    public bool LoosArgument { get; }
    Task Dispatch(DispatchableJobArgument argument, object? originalMessage = null);
}
public interface IRetriable
{

    public void Successful() => RetryPolicyPrototype.SetLastAttemptResult(JobCompletionResult.SUCCESSFUL);
    public void Failure() => RetryPolicyPrototype.SetLastAttemptResult(JobCompletionResult.FAILURE);
    public JobCompletionResult JobCompletionResult => RetryPolicyPrototype.GetLastAttemptResult();
    RetryManager RetryPolicyPrototype { get; set; }
    string GetRetryState => RetryPolicyPrototype.GetCurrentStateJson();
}

public enum RetryType
{
    /// <summary>
    /// As soon as possible, rerun the current failed job 
    /// </summary>
    ASAP = 1,
    /// <summary>
    /// as late as possible, move the failed message to the end of the queue
    /// </summary>
    ALAP = 2,
    /// <summary>
    /// Defer the message to QueDuler internal defer jobs manager, so the job can rerun job with a custom logic (Circuit breaker, exponential time, general logic and logging etc...) and time
    /// </summary>
    DEFER = 3,
    NONE = 4
}
public class RetryManager
{
    public RetryManager(IDispatchableJob job)
    {
        this.job = job;
    }
    internal RetryPolicy retryPolicy = new();
    private IDispatchableJob job;

    public RetryManager SetASAPCount(int count)
    {
        retryPolicy.ASAP_Count = count;
        return this;
    }
    public RetryManager SetALAPCount(int count)
    {
        retryPolicy.ALAP_Count = count;
        return this;
    }
    public RetryManager AddDeferStage(TimeSpan at)
    {
        retryPolicy.DeferStages.Add(new(at, false));
        return this;
    }
    public RetryType GetCurrentRetryActionType() => retryPolicy.GetCurrentRetryActionType();

    public string GetCurrentStateJson() => JsonSerializer.Serialize(retryPolicy);
    public JobCompletionResult GetLastAttemptResult() => retryPolicy.LastAttemptResult;
    public async Task Retry(Func<Task> retry)
    {
        await retryPolicy.Retry(retry);
    }
    public static RetryManager Parse(string json, IDispatchableJob job)
    {
        var t = JsonSerializer.Deserialize<RetryPolicy>(json);
        var n = new RetryManager(job);
        n.retryPolicy = t;
        return n;
    }

    internal void SetLastAttemptResult(JobCompletionResult sUCCESSFUL)
    {
        retryPolicy.LastAttemptResult = sUCCESSFUL;
    }
}
internal class RetryPolicy
{
    public DateTime LastAttempt { get; set; } = DateTime.MinValue;
    public int ASAP_Count { get; set; }
    public int ASAP_Index { get; set; }
    public int ALAP_Count { get; set; }
    public int ALAP_Index { get; set; }
    public List<DeferStage> DeferStages { get; set; }
    public bool HasRetry { get; set; }
    public JobCompletionResult LastAttemptResult { get; set; } = JobCompletionResult.NOT_YET;
    internal RetryType GetCurrentRetryActionType()
    {
        if (ASAP_Count > ASAP_Index)
            return RetryType.ASAP;
        if (ALAP_Count > ALAP_Index)
            return RetryType.ALAP;
        if (DeferStages?.Any(a => !a.IsDone) == true)
            return RetryType.DEFER;
        return RetryType.NONE;
    }
    public async Task<bool> Retry(Func<Task> retry)
    {
        LastAttempt = DateTime.UtcNow;
        try
        {
            switch (GetCurrentRetryActionType())
            {
                case RetryType.ASAP:
                    ASAP_Index++;
                    await retry();
                    break;
                case RetryType.ALAP:
                    ALAP_Index++;
                    await retry();
                    break;
                case RetryType.DEFER:
                    DeferStages.First(a => !a.IsDone).IsDone = true;
                    //TODO
                    break;
                case RetryType.NONE:
                default:
                    return false;
            }
        }
        catch (Exception)
        {
            throw;
        }
        return true;
    }

    public class DeferStage
    {
        public DeferStage(TimeSpan at, bool isDone)
        {
            At = at;
            IsDone = isDone;
        }
        public bool IsDone { get; set; }
        public TimeSpan At { get; set; }
    }
}
public enum JobCompletionResult
{
    NOT_YET,
    SUCCESSFUL,
    FAILURE,
}