// SPDX-License-Identifier: LGPL-3.0-only

using System.Diagnostics;
using Hemati.DependencyInjection.Implementation.ServiceDescriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Hemati.DependencyInjection.Implementation;

public class ServiceResolver : IServiceProviderExtended, IServiceScopeFactory, ISpCloneCreator, IDisposable
{
 public ServiceResolver(IServiceCollection serviceDescriptors, PrecomputedServiceDescriptionData[] precomputedServiceDescription, ServiceActivator activator)
 {
  if (serviceDescriptors is null)
  {
   throw new ArgumentNullException(nameof(serviceDescriptors));
  }

  if (activator is null)
  {
   throw new ArgumentNullException(nameof(activator));
  }

  Root = new(activator, ScopeRole.RootScope);

  ServiceDescriptor sp = new(
   typeof(IServiceProvider),
   p =>
   {
    return p switch
    {
     ServiceResolver r => r,
     ScopeCache c => c,
     _ => throw new InvalidOperationException()
    };
   },
   ServiceLifetime.Transient);
  ServiceDescriptor spExtended = new(
   typeof(IServiceProviderExtended),
   p =>
   {
    return p switch
    {
     ServiceResolver r => r,
     ScopeCache c => c,
     _ => throw new InvalidOperationException()
    };
   },
   ServiceLifetime.Transient);
  ServiceDescriptor scf = new(
   typeof(IServiceScopeFactory),
   static p =>
   {
    if (p is IServiceScopeFactory sf)
    {
     return sf;
    }

    throw new InvalidOperationException("Current service provider is not a IServiceScopeFactory");
   },
   ServiceLifetime.Transient);
  ServiceDescriptor sc = new(
   typeof(IServiceScope),
   static p =>
   {
    return p switch
    {
     ScopeCache scopeCache => scopeCache,
     ServiceResolver => null!,
     _ => throw new InvalidOperationException($"scope was requested, but resolver was of unknown type: {p?.GetType()}")
    };
   },
   ServiceLifetime.Transient);
  ServiceDescriptor cwc = new(
   typeof(IConnectionWideCache),
   static p =>
   {
    return p switch
    {
     ScopeCache scopeCache => (IConnectionWideCache)scopeCache,
     ServiceResolver sr => sr.Root,
     _ => throw new InvalidOperationException($"connection wide cache was requestd but resolver was of unknown type: {p?.GetType()}")
    };
   },
   ServiceLifetime.Transient);
  ServiceDescriptor cc = new(
   typeof(ISpCloneCreator),
   static sp =>
   {
    if (sp is ServiceResolver sr)
    {
     return sr;
    }

    throw new InvalidOperationException("Can't use ISpCloneCreator in a scope");
   },
   ServiceLifetime.Transient);

  activator.Descriptor.Populate(sp);
  activator.Descriptor.Populate(spExtended);

  activator.Descriptor.Populate(scf);
  activator.Descriptor.Populate(sc);
  activator.Descriptor.Populate(cwc);
  activator.Descriptor.Populate(cc);

  activator.Descriptor.Populate(serviceDescriptors);
  activator.Descriptor.Populate(precomputedServiceDescription);
 }

 private ServiceResolver(ServiceActivator activator)
 {
  Root = new(activator, ScopeRole.RootScope);
 }

 internal ScopeCache Root { get; }

 public ServiceActivator Activator => Root.Activator;

 public IServiceScope CreateScope()
 {
  ScopeCache scope = Root.CopyKeep(ScopeRole.ParentScope, CacheScope.Singleton);
  return scope;
 }

 public object? GetService(Type serviceType) => Root.GetService(new(serviceType), this);

 public object? SatisfyImports(object? service) => Root.SatisfyImports(service, this);

 public void Populate(PrecomputedServiceDescriptionData[] serviceDescriptions) => Root.Populate(serviceDescriptions);

 public void Depopulate(string tag) => Root.Depopulate(tag);

 public void Populate(IServiceCollection serviceCollection) => Root.Populate(serviceCollection);

 public IEnumerable<IServiceDescription> GetCurrentlyRegisteredServiceDescriptions() => Root.GetCurrentlyRegisteredServiceDescriptions();

 public object? GetExportedValue(Type service, string? contract = null) => Root.GetExportedValue(service, contract);

 public void ClearAllBuildCaches() => Root.ClearAllBuildCaches();

 public void Dispose()
 {
  try
  {
   Root.Dispose();
  }
  catch (Exception)
  {
#if DEBUG
   Debugger.Break();
#endif
  }
 }

 public IServiceProvider Clone(IEnumerable<ServiceDescriptor> descriptorsToReplace)
 {
  ServiceActivator activator = Root.Activator.Clone(descriptorsToReplace);
  return new ServiceResolver(activator);
 }
}