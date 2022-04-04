// See https://aka.ms/new-console-template for more information
using System.Text.Json;

public partial class Dispatcher
{
    public abstract class BaseArgument
    {
        public override string ToString() => JsonSerializer.Serialize(this);
    }
}
