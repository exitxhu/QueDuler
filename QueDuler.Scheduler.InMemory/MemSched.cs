
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace QueDuler;
public class MemSched
{
    private readonly InMemorySchedulerOptions _option;
    private ObservableCollection<string> _log;
    private readonly IServiceProvider _provider;

    private HashSet<PersistedJob> Jobs;

    public MemSched(IServiceProvider provider,
        InMemorySchedulerOptions option = null)
    {
        option = option ?? new InMemorySchedulerOptions();
        _log = new ObservableCollection<string>();
        _log.CollectionChanged += _log_CollectionChanged;
        this._option = option;
        Jobs = new HashSet<PersistedJob>(comparer: PersistedJob.GetComparer());
        _provider = provider;
        var coor = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(option.TickTimeMillisecond);
                HashSet<PersistedJob>? js = Jobs.Where(a => !a.IsLocked && a.IsScheduleMeet).ToHashSet();
                var tsk = js.Select(a => Task.Run(async () =>
                {
                    var pr = _provider.CreateScope().ServiceProvider.GetService(a.Job.GetType()) as ISchedulableJob;
                    if (pr != null)
                    {
                        a.IsLocked = true;
                        try
                        {
                            await pr.Do();
                        }
                        catch (Exception ex)
                        {
                        }
                        finally
                        {
                            a.IsLocked = false;

                        }
                    }
                }));
                Task.WhenAll(tsk).ConfigureAwait(false);
            }
        });
    }

    private void _log_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add) return;

        var c = e.NewStartingIndex - _option.MaxInMemoryLogCount - 1;
        if (c > 0)
        {
            var s = sender as ObservableCollection<string>;
            for (int i = 0; i < c; i++)
            {
                s.RemoveAt(0);
            }
        }
    }

    public void Do()
    {


    }

    internal void AddOrUpdate(ISchedulableJob job)
    {
        var j = new PersistedJob
        {
            Job = job,
        };
        Jobs.Add(j);
    }
}
