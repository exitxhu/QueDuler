// See https://aka.ms/new-console-template for more information
using System.Text.Json;
using System.Text.Json.Serialization;

public class DispatchableJobArgument : BaseArgument
{
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
    [JsonInclude]
    public bool IsRetry { get; internal set; } = false;
    [JsonInclude]
    public string JobId { get; internal set; }
    [JsonInclude]
    public string RetryState { get; internal set; }
    public bool IsBroadCast { get; }
    public object ArgumentObject { get; }
    public static bool TryParse(string json, out DispatchableJobArgument argument)
    {
        try
        {
            argument = System.Text.Json.JsonSerializer.Deserialize<DispatchableJobArgument>(json);
            return !string.IsNullOrEmpty(argument.JobId);
        }
        catch (Exception)
        {
            argument = null;
            return false;
        }
    }

    public override string ToJson() => JsonSerializer.Serialize(this);
}

