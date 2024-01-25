using System.Reflection;

namespace QueDuler.Core.Internals;

public class QuedulerOptions
{
    public HashSet<object> BrokerKeys { get; set; }=new HashSet<object>();
    public List<Assembly> JobAssemblies { get; set; } = new List<Assembly>();
}
