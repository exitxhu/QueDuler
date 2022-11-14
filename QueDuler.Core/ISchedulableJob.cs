public interface ISchedulableJob
{
    public TimeZoneInfo TimeZoneInfo();
    public string JobId { get; }
    public string Cron { get; }
    Task Do(params object[] arguments);
}
