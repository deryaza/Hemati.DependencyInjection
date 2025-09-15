// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Concurrent;
using Hemati.DependencyInjection.Implementation.Core;
using Hemati.DependencyInjection.Implementation.ServiceDescriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Hemati.DependencyInjection.Implementation;

public partial class ScopeCache : IServiceProviderExtended, IServiceScope, IConnectionWideCache
{
 public const int DefaultImplementationNumber = 0;

 public readonly record struct CacheEntry(BaseServiceKey BaseServiceKey, int ImplementationNumber);

 private readonly ConcurrentDictionary<CacheScope, ConcurrentDictionary<CacheEntry, object?>> _cacheEntries;
 private readonly ScopeRole _scopeRole;

 public ScopeCache(ServiceActivator activator, ScopeRole scopeRole)
  : this(activator, new(), scopeRole)
 {
 }

 private ScopeCache(ServiceActivator activator, ConcurrentDictionary<CacheScope, ConcurrentDictionary<CacheEntry, object?>> cacheEntries, ScopeRole scopeRole)
 {
  _scopeRole = scopeRole;
  Activator = activator ?? throw new ArgumentNullException(nameof(activator));
  _cacheEntries = cacheEntries ?? throw new ArgumentNullException(nameof(cacheEntries));
  foreach (object? v in Enum.GetValues(typeof(CacheScope)))
  {
   if (v == null)
   {
    continue;
   }

   CacheScope scope = (CacheScope)v;
   if (cacheEntries.ContainsKey(scope))
   {
    continue;
   }

   _cacheEntries[scope] = new();
  }
 }

 public IServiceProvider ServiceProvider => this;

 public ServiceActivator Activator { get; }



 public object? GetActivatedService(CacheScope scope, BaseServiceKey serviceType, int implementationNo)
 {
  ConcurrentDictionary<CacheEntry, object?> a = _cacheEntries[scope];
  return a[new(serviceType, implementationNo)];
 }

 public CacheScope IsAlreadyActivated(BaseServiceKey findService, int implementationNo)
 {
  CacheEntry key = new(findService, implementationNo);
  foreach ((CacheScope scope, ConcurrentDictionary<CacheEntry, object?> cache) in _cacheEntries)
  {
   if (cache.ContainsKey(key))
   {
    return scope;
   }
  }

  return CacheScope.None;
 }

 public object? Store(object? implementation, BaseServiceKey serviceType, int implementationNo, CacheScope scope)
 {
  if (_scopeRole == ScopeRole.RootScope
   && ((scope & CacheScope.Scoped) != 0 || (scope & CacheScope.ConnectionCache) != 0 || (scope & CacheScope.ConnectionWide) != 0))
  {
   // видимо, это вполне валидно в аспнете, ну ок
   // throw new InvalidOperationException($"Attempt to use scoped service {serviceType} in root resolver.");
  }

  // if (scope == CacheScope.Transient && _scopeRole == ScopeRole.RootScope)
  if (scope == CacheScope.Transient)
  {
   return implementation;
  }

  ConcurrentDictionary<CacheEntry, object?> ce = _cacheEntries.GetOrAdd(scope, _ => new());
  ce[new(serviceType, implementationNo)] = implementation;
  return implementation;
 }

 public ScopeCache CopyKeep(ScopeRole scopeRole, CacheScope scope)
 {
  return new(
   Activator,
   new(_cacheEntries.Where(kv => scope.HasFlag(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value)),
   scopeRole
  );
 }



 public object? GetService(FindServiceRequest findServiceType, IServiceProvider caller) => Activator.GetService(findServiceType, this, caller);

 public object? GetService(Type serviceType) => GetService(new(serviceType), this);

 public object? GetExportedValue(Type service, string? contract = null) => GetService(new(service) { StringContract = contract }, this);


 public object? SatisfyImports(object? service) => SatisfyImports(service, this);

 public object? SatisfyImports(object? service, IServiceProvider serviceProvider) => Activator.SatisfyImports(service, serviceProvider);

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

  foreach ((CacheScope cacheScope, ConcurrentDictionary<CacheEntry, object?> cache) in _cacheEntries)
  {
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

  _cacheEntries[CacheScope.ConnectionCache][new(new(typeof(T), null), DefaultImplementationNumber)] = service;
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
   if (!_cacheEntries[CacheScope.ConnectionCache].ContainsKey(new(new(loadServiceContract, null), DefaultImplementationNumber)))
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