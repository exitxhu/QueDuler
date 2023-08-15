namespace QueDuler;

public class InMemorySchedulerOptions
{
    private int tickTimeMillisecond = 1000;

    public int MaxInMemoryLogCount { get; set; } = 10;
    /// <summary>
    /// how long should wait till next job triggers, between 0.5 - 10 sec, default 1
    /// </summary>
    public int TickTimeMillisecond { get => tickTimeMillisecond; set => tickTimeMillisecond = value < 500 ? 500 : value > 10_000 ? 10_000 : value; }
}
