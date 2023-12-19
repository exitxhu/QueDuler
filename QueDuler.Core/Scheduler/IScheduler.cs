public interface IScheduler
{
    public Task Schedule(ISchedulableJob job);
}
