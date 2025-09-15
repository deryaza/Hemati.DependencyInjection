// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Hemati.DependencyInjection;

public interface IConnectionWideCache
{
 void StoreObj<T>(T service);

 void EnsureEachSatisfied();
}

public static class ConnectionWideCacheExtension
{
 public static IServiceCollection AddConnectionWide<T>(this IServiceCollection serviceCollection)
 {
  return AddConnectionWide<T, T>(serviceCollection);
 }

 public static IServiceCollection AddConnectionWide<TContract, TImplementation>(this IServiceCollection serviceCollection)
 {
  serviceCollection.Add(new ConnectionWideServiceDescriptor(typeof(TContract), typeof(TImplementation)));
  return serviceCollection;
 }

 public static IServiceCollection PromiseToAddScoped<TContract>(this IServiceCollection serviceCollection)
 {
  serviceCollection.Add(new PromiseToAddServiceDescriptor(typeof(TContract)));
  return serviceCollection;
 }
}

public sealed class PromiseToAddServiceDescriptor : ServiceDescriptor
{
 public PromiseToAddServiceDescriptor(Type serviceType)
  : base(serviceType, _ => throw new InvalidOperationException("this descriptor must be special cased"), (ServiceLifetime)HbServiceLifetime.ConnectionCache)
 {
 }
}

public sealed class ConnectionWideServiceDescriptor : ServiceDescriptor
{
 public ConnectionWideServiceDescriptor(Type serviceType, Type connectionWideImplementationType)
  : base(serviceType, connectionWideImplementationType, (ServiceLifetime)HbServiceLifetime.ConnectionWide)
 {
 }
}