using NCrontab;
using System.Diagnostics.CodeAnalysis;

namespace QueDuler;

class PersistedJob
{
    public CrontabSchedule Scheduler => CrontabSchedule.Parse(Job.Cron, new CrontabSchedule.ParseOptions { IncludingSeconds = Job.Cron.Split(" ").Length > 5 ? true : false });
    private DateTime? _nextSchedule = null;
    public bool IsScheduleMeet
    {
        get
        {

            if (!_nextSchedule.HasValue)
            {
                _nextSchedule = Scheduler.GetNextOccurrence(DateTime.Now);
                return _nextSchedule.Value < DateTime.Now;
            }
            else if (_nextSchedule.Value > DateTime.Now)
            {
                return false;
            }
            else
            {
                _nextSchedule = Scheduler.GetNextOccurrence(DateTime.Now);
                return true;
            }
        }
    }

    public ISchedulableJob Job { get; internal set; }
    public bool IsLocked { get; set; }


    public static bool operator ==(PersistedJob left, PersistedJob right) => CheckEquals(left, right);
    public static bool operator !=(PersistedJob left, PersistedJob right) => !CheckEquals(left, right);
    internal static PersistedJobComparer GetComparer() => new PersistedJobComparer();
    static bool CheckEquals(PersistedJob? x, PersistedJob? y)
    {
        return x.Job.JobId == y.Job.JobId;
    }
    internal class PersistedJobComparer : IEqualityComparer<PersistedJob>
    {
        public bool Equals(PersistedJob? x, PersistedJob? y)
        {
            return CheckEquals(x, y);
        }
        public int GetHashCode([DisallowNull] PersistedJob obj)
        {
            var t = new HashCode();
            t.Add(obj.Job.JobId);
            return t.ToHashCode();
        }
    }

}
