using System.Reflection;

namespace QueDuler.Core.Internals;

public class QuedulerOptions
{
    public List<Assembly> JobAssemblies { get; set; } = new List<Assembly>();
}
