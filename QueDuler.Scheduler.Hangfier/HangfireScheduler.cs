using Hangfire;

namespace QueDuler;

public class HangfireScheduler : IScheduler
{
    private readonly IRecurringJobManager recurringJob;

        public HangfireScheduler(IRecurringJobManager recurringJob)
        {
            this.recurringJob = recurringJob;
        }
        public async Task Schedule(ISchedulableJob job)
        {
            recurringJob.AddOrUpdate(job.JobId, () => job.Do(), job.Cron, job.TimeZoneInfo() ?? TimeZoneInfo.Utc);
        }
    }
}
