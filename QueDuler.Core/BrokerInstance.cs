using Microsoft.Extensions.DependencyInjection;

public abstract class BrokerInstance<T,U>
{
    public abstract T BrokerConfig { get; set; }
    public abstract IEnumerable<U> PathConfigs { get; set; }
    public abstract ServiceLifetime BrokerLifetime { get; set; }
}

