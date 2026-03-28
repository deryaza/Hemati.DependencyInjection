// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using Hemati.DependencyInjection.Implementation.Core;
using Hemati.DependencyInjection.Implementation.ServiceDescriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Hemati.DependencyInjection.Implementation;

public partial class ScopeCache : IServiceProviderExtended, IServiceScope, IConnectionWideCache
{
    public const int DefaultImplementationNumber = 0;

    public readonly struct CacheEntry(BaseServiceKey baseServiceKey, int implementationNumber) : IEquatable<CacheEntry>
    {
        private readonly BaseServiceKey _baseServiceKey = baseServiceKey;
        private readonly int _implementationNumber = implementationNumber;

        public bool Equals(CacheEntry other)
        {
            return _baseServiceKey.Equals(other._baseServiceKey) && _implementationNumber == other._implementationNumber;
        }

        public override bool Equals(object? obj)
        {
            return obj is CacheEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _baseServiceKey.GetHashCode();
        }
    }

    private readonly ConcurrentDictionary<CacheEntry, object?>[] _cacheEntries;
    private readonly ScopeRole _scopeRole;

    private static ConcurrentDictionary<CacheEntry, object?>[] CreateRootCache()
    {
        var entries = new ConcurrentDictionary<CacheEntry, object?>[4];
        entries[CacheScopeIndexes.Singleton] = new();
        entries[CacheScopeIndexes.Scoped] = new();
        entries[CacheScopeIndexes.ConnectionWide] = new();
        entries[CacheScopeIndexes.ConnectionCache] = new();
        return entries;
    }

    public ScopeCache(ServiceActivator activator, ScopeRole scopeRole, ServiceResolver rootResolver)
        : this(activator, CreateRootCache(), scopeRole, rootResolver)
    {
        RootResolver = rootResolver;
    }

    private ScopeCache(ServiceActivator activator, ConcurrentDictionary<CacheEntry, object?>[] cacheEntries, ScopeRole scopeRole, ServiceResolver rootResolver)
    {
        RootResolver = rootResolver;
        _scopeRole = scopeRole;
        Activator = activator ?? throw new ArgumentNullException(nameof(activator));
        _cacheEntries = cacheEntries ?? throw new ArgumentNullException(nameof(cacheEntries));
    }

    internal ServiceResolver RootResolver { get; }

    public IServiceProvider ServiceProvider => this;

    public ServiceActivator Activator { get; }

    public bool TryGetActivatedService(CacheScope scope, BaseServiceKey serviceKey, int implementationNo, out object? implementation)
    {
        CacheEntry key = new(serviceKey, implementationNo);
        var scopeEntries = _cacheEntries[CacheScopeIndexes.ToIndex(scope)];
        if (!scopeEntries.TryGetValue(key, out implementation))
        {
            Lock getServiceLock = new Lock();
            getServiceLock.Enter();

#pragma warning disable CS9216 // A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement
            var res = scopeEntries.GetOrAdd(key, getServiceLock);
#pragma warning restore CS9216 // A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement
            if (res == getServiceLock)
            {
#pragma warning disable CS9216 // A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement
                implementation = getServiceLock;
#pragma warning restore CS9216 // A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement
                return false;
            }

            getServiceLock.Exit();
            implementation = res;
        }

        if (implementation is Lock l)
        {
            l.Enter();

            var createdService = scopeEntries[key];
            if (createdService == l)
            {
#pragma warning disable CS9216 // A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement
                implementation = l;
#pragma warning restore CS9216 // A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement
                return false;
            }

            implementation = createdService;
        }

        return true;
    }

    public void Store(object? implementation, BaseServiceKey serviceType, int implementationNo, CacheScope scope)
    {
        Debug.Assert(scope != CacheScope.Transient);

        ConcurrentDictionary<CacheEntry, object?> ce = _cacheEntries[CacheScopeIndexes.ToIndex(scope)];
        ce[new(serviceType, implementationNo)] = implementation;
    }

    public ScopeCache CopyKeep(ScopeRole scopeRole, CacheScope scope)
    {
        var entries = new ConcurrentDictionary<CacheEntry, object?>[4];

        entries[CacheScopeIndexes.Singleton] = (scope & CacheScope.Singleton) != 0 ? _cacheEntries[CacheScopeIndexes.Singleton] : new();
        entries[CacheScopeIndexes.Scoped] = (scope & CacheScope.Scoped) != 0 ? _cacheEntries[CacheScopeIndexes.Scoped] : new();
        entries[CacheScopeIndexes.ConnectionWide] = (scope & CacheScope.ConnectionWide) != 0 ? _cacheEntries[CacheScopeIndexes.ConnectionWide] : new();
        entries[CacheScopeIndexes.ConnectionCache] = (scope & CacheScope.ConnectionCache) != 0 ? _cacheEntries[CacheScopeIndexes.ConnectionCache] : new();

        // TODO: optimize
        return new(
            Activator,
            entries,
            scopeRole,
            RootResolver
        );
    }


    public object? GetService(FindServiceRequest findServiceType, IServiceProviderExtended caller) => Activator.GetService(findServiceType, this, caller);

    public object? GetService(Type serviceType) => GetService(new(serviceType), this);

    public object? GetExportedValue(Type service, string? contract = null) => GetService(new(service) { StringContract = contract }, this);


    public void SatisfyImports(object? service) => SatisfyImports(service, this);

    public void SatisfyImports(object? service, IServiceProvider serviceProvider) => Activator.SatisfyImports(service, serviceProvider);

    public void Populate(PrecomputedServiceDescriptionData[] serviceDescriptions) => Activator.Descriptor.Populate(serviceDescriptions);

    public void Depopulate(string tag) => Activator.Descriptor.Depopulate(tag);

    public void Populate(IServiceCollection serviceCollection) => Activator.Descriptor.Populate(serviceCollection);

    public IEnumerable<IServiceDescription> GetCurrentlyRegisteredServiceDescriptions() => Activator.Descriptor.GetDescriptions();

    public void ClearAllBuildCaches() => Activator.ClearCaches();

    #region IDisposable

    private int _disposingIfOne;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposingIfOne, 1) == 1)
        {
            return;
        }

        CacheScope toDispose = _scopeRole switch
        {
            ScopeRole.RootScope => CacheScope.Singleton | CacheScope.Scoped | CacheScope.Transient | CacheScope.ConnectionWide,
            ScopeRole.ParentScope => CacheScope.Scoped | CacheScope.Transient | CacheScope.ConnectionCache | CacheScope.ConnectionWide,
            ScopeRole.ChildScope => CacheScope.Scoped | CacheScope.Transient,
            _ => throw new ArgumentOutOfRangeException()
        };

        for (var index = 0; index < _cacheEntries.Length; index++)
        {
            ConcurrentDictionary<CacheEntry, object?> cache = _cacheEntries[index];
            var cacheScope = CacheScopeIndexes.ToScope(index);
            if ((toDispose & cacheScope) == 0)
            {
                continue;
            }

            foreach ((_, object? implementation) in cache)
            {
                try
                {
                    if (implementation is IDisposable disposableImplementation)
                    {
                        disposableImplementation.Dispose();
                    }
                    else if (implementation is IAsyncDisposable asyncDisposableImplementation)
                    {
                        asyncDisposableImplementation.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    }
                }
                catch (Exception)
                {
                    // Dispose shouldn't throw
                }
            }
        }

        Interlocked.Exchange(ref _disposingIfOne, 0);
    }

    #endregion
}

public partial class ScopeCache
{
    private IEnumerable<IServiceDescription> EnumerateCachedParameters()
    {
        return Activator.Descriptor.GetDescriptions().Where(x => x.GetServiceScope() == HbServiceLifetime.ConnectionCache);
    }

    public void StoreObj<T>(T service)
    {
        if (_scopeRole == ScopeRole.RootScope)
        {
            throw new InvalidOperationException("Root scope are not allowed to utilize store parameters");
        }

        if (!EnumerateCachedParameters().Any(par => par.IsSameContractType(typeof(T))))
        {
            throw new InvalidOperationException($"Parameter of type {typeof(T)} was not registered as a cachedobjparameter");
        }

        _cacheEntries[CacheScopeIndexes.ToIndex(CacheScope.ConnectionCache)][new(new(typeof(T), null), DefaultImplementationNumber)] = service;
    }

    public void EnsureEachSatisfied()
    {
        if (_scopeRole == ScopeRole.RootScope)
        {
            throw new InvalidOperationException("Root scopes are not allowed to utilize store parameters");
        }

        foreach (IServiceDescription description in EnumerateCachedParameters())
        {
            Type loadServiceContract = description.LoadServiceContract();
            if (!_cacheEntries[CacheScopeIndexes.ToIndex(CacheScope.ConnectionCache)].ContainsKey(new(new(loadServiceContract, null), DefaultImplementationNumber)))
            {
                throw new InvalidOperationException($"service of type {loadServiceContract} was not cached on scope");
            }
        }
    }
}

public partial class ScopeCache : IServiceScopeFactory
{
    public IServiceScope CreateScope()
    {
        ScopeCache scope = CopyKeep(ScopeRole.ChildScope, CacheScope.Singleton | CacheScope.ConnectionCache | CacheScope.ConnectionWide);
        return scope;
    }
}