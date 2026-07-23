using System;
using System.Collections.Generic;

public sealed class ServiceRegistry
{
    private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

    public void Register<TService>(TService service) where TService : class
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        services[typeof(TService)] = service;
    }

    public TService Get<TService>() where TService : class
    {
        if (TryGet(out TService service))
        {
            return service;
        }

        throw new InvalidOperationException($"Service '{typeof(TService).Name}' has not been registered.");
    }

    public bool TryGet<TService>(out TService service) where TService : class
    {
        if (services.TryGetValue(typeof(TService), out object rawService))
        {
            service = rawService as TService;
            return service != null;
        }

        service = null;
        return false;
    }

    public bool Contains<TService>() where TService : class
    {
        return services.ContainsKey(typeof(TService));
    }

    public bool Unregister<TService>(TService service) where TService : class
    {
        if (service == null)
        {
            return false;
        }

        if (!services.TryGetValue(typeof(TService), out object registeredService))
        {
            return false;
        }

        if (!ReferenceEquals(registeredService, service))
        {
            return false;
        }

        return services.Remove(typeof(TService));
    }
}
