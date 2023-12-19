public interface IObservableJob
{
    public string JobPath { get; }

    Task OnNext(object? originalMessage = null);
    Task OnError(Exception ex);
    Task OnComplete();
}

