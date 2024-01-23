// See https://aka.ms/new-console-template for more information

using Newtonsoft.Json;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("QueDuler.Core.Test")]
public class DispatchableJobArgument : BaseArgument
{
    [JsonConstructor]
    public DispatchableJobArgument(string jobId, object argumentObject = null, bool isBroadCast = false)
    {
        JobId = jobId;
        ArgumentObject = argumentObject;
        IsBroadCast = isBroadCast;
    }
    internal DispatchableJobArgument GetRetryObjectForJob(string jobId, string retryState)
    {
        var t = this.MemberwiseClone() as DispatchableJobArgument;
        t.IsRetry = true;
        t.JobId = jobId;
        t.RetryState = retryState;
        return t;

    }
    [JsonProperty]
    public bool IsRetry { get; internal set; } = false;
    [JsonProperty]
    public string JobId { get; internal set; }
    [JsonProperty]
    public string RetryState { get; internal set; }
    [JsonProperty]
    public bool IsBroadCast { get; }
    [JsonProperty]
    public object ArgumentObject { get; }
    public static bool TryParse(string json, out DispatchableJobArgument argument, out Exception? ex)
    {
        try
        {
            argument = JsonConvert.DeserializeObject<DispatchableJobArgument>(json);
            ex = default;
            return !string.IsNullOrEmpty(argument.JobId);
        }
        catch (Exception exc)
        {
            ex = exc;
            argument = null;
            return false;
        }

    }
    public DispatchableJobArgument()
    {

    }
    public override string ToJson() => JsonConvert.SerializeObject(this);
}

