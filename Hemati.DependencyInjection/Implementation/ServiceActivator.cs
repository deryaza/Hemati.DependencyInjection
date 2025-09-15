// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Concurrent;
using Hemati.DependencyInjection.Implementation.Parameters;
using Hemati.DependencyInjection.Implementation.ServiceDescriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Hemati.DependencyInjection.Implementation;

public partial class ServiceActivator
{
 private readonly ConcurrentDictionary<FindServiceRequest, Func<ScopeCache, IServiceProvider, object?>> _implementations;
 private readonly Func<FindServiceRequest, Func<ScopeCache, IServiceProvider, object?>> _buildFunc;
 private readonly DependentPropertiesSetter _dependentPropertiesSetter = new();

 public readonly IServiceBuilder Builder;
 public readonly ServicesDescriptor Descriptor;

 public ServiceActivator(IServiceBuilder builder, ServicesDescriptor descriptor)
 {
  _implementations = new();
  _buildFunc = Build; // cached, so no allocations on each GetService call

  Builder = builder;
  Descriptor = descriptor;
 }

 protected virtual Func<ScopeCache, IServiceProvider, object?> BuildCore(Parameter parameter) => Builder.Build(parameter);

 private Func<ScopeCache, IServiceProvider, object?> Build(FindServiceRequest type) =>
  Descriptor.TryGetParameter(null, type) is (var parameter, _)
   ? BuildCore(parameter)
   : BuildCore(new UnknownParameter(type.ServiceType, Core.HbServiceLifetime.Transient));

 public virtual object? GetService(FindServiceRequest findServiceType, ScopeCache cache, IServiceProvider caller)
 {
  return cache.IsAlreadyActivated(findServiceType.ToBaseServiceKey(), ScopeCache.DefaultImplementationNumber) switch
  {
   CacheScope.None => _implementations.GetOrAdd(findServiceType, _buildFunc)(cache, caller),
   var scope => cache.GetActivatedService(scope, findServiceType.ToBaseServiceKey(), ScopeCache.DefaultImplementationNumber)
  };
 }

 internal object? GetService(IServiceDescription serviceDescription, int implementationNumber, ScopeCache cache, IServiceProvider caller)
 {
  return cache.IsAlreadyActivated(serviceDescription.GetBaseServiceKey(), implementationNumber) switch
  {
   CacheScope.None => BuildCore(Descriptor.CreateParameter(serviceDescription))(cache, caller),
   var scope => cache.GetActivatedService(scope, serviceDescription.GetBaseServiceKey(), implementationNumber)
  };
 }

 internal object? SatisfyImports(object? service, IServiceProvider serviceProvider)
 {
  if (service == null)
  {
   return service;
  }

  _dependentPropertiesSetter.SetFields(serviceProvider, service, service.GetType());
  return service;
 }

 public void ClearCaches()
 {
  _implementations.Clear();
  _dependentPropertiesSetter.Clear();
 }

 public ServiceActivator Clone(IEnumerable<ServiceDescriptor> descriptorsToReplace)
 {
  return new(Builder, Descriptor.Clone(descriptorsToReplace));
 }
}