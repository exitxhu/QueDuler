// See https://aka.ms/new-console-template for more information
using System.Text.Json;

public partial class Dispatcher
{
    public class DispatchableJobArgument : BaseArgument
    {
        public DispatchableJobArgument(string jobId, object argumentObject = null)
        {
            JobId = jobId;
            ArgumentObject = argumentObject;
        }
        public string JobId { get; }
        public object ArgumentObject { get; }
        public static DispatchableJobArgument Parse(string json) => JsonSerializer.Deserialize<DispatchableJobArgument>(json);
    }
}
