// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json;

public abstract class BaseArgument
{
    public abstract string ToJson();
}
public class DispatchableJobArgument : BaseArgument
{
    public DispatchableJobArgument(string jobId, object argumentObject = null, bool isBroadCast = false)
    {
        JobId = jobId;
        ArgumentObject = argumentObject;
        IsBroadCast = isBroadCast;
    }
    public bool IsBroadCast { get; }
    public string JobId { get; }
    public object ArgumentObject { get; }
    public static bool TryParse(string json, out DispatchableJobArgument argument)
    {
        try
        {
            argument = JsonConvert.DeserializeObject<DispatchableJobArgument>(json);
            return true;
        }
        catch (Exception)
        {
            argument = null;
            return false;
        }
    }

    public override string ToJson() => JsonConvert.SerializeObject(this);
}
