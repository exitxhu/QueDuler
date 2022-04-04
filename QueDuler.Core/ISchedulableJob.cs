public interface ISchedulableJob
{
    public string JobId { get; }
    public string Cron { get; }
    Task Do(params object[] arguments);
}
