using System.Diagnostics.CodeAnalysis;

namespace QueDuler;

public class InMemoryScheduler : IScheduler
{
    private readonly MemSched _memSched;

    public InMemoryScheduler(MemSched memSched)
    {
        _memSched = memSched;
    }

    public async Task Schedule(ISchedulableJob job)
    {
        _memSched.AddOrUpdate(job);
    }
}

