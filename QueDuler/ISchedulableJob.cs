public interface ISchedulableJob
{
    public string JobId { get; }
    public string Cron { get; }
    void Do(params object[] arguments);
}
