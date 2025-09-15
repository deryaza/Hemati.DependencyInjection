// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;

namespace Hemati.DependencyInjection.Implementation.ServiceDescriptions;

public abstract class ServiceDescriptionBase(string? stringContract = null) : IServiceDescription
{
 public virtual string? Tag => null;
 public virtual bool HasMetadata => false;
 public virtual bool IsPromiseToAddServiceDescriptor => false;
 public virtual bool IsImplementationFactory => false;
 public virtual bool IsImplementationInstance => false;
 public virtual bool IsImplementationType => false;
 public virtual bool IsEnumerableType => false;
 public virtual string? StringContract => stringContract;
 public virtual bool SatisfiesStringContract(string? contract) => string.Equals(stringContract, contract, StringComparison.OrdinalIgnoreCase);
 public virtual bool IsSameContractType(Type type) => GetBaseServiceKey().Equals(new(type, null));

 public virtual Dictionary<string, object?> GetMetadata() => new();

 public abstract BaseServiceKey GetBaseServiceKey();
 public abstract Type LoadServiceContract();

 public abstract HbServiceLifetime GetServiceScope();

 protected virtual Func<IServiceProvider, object?> LoadFactoryCore()
 {
  throw new InvalidOperationException("Someone lied in ServiceDescriptionBase");
 }

 public virtual Func<IServiceProvider, object?> LoadFactory()
 {
  if (!IsImplementationFactory)
  {
   throw new InvalidOperationException("Current description is not IsImplementationFactory");
  }

  return LoadFactoryCore();
 }

 protected virtual object LoadImplementationInstanceCore()
 {
  throw new InvalidOperationException("Someone lied in ServiceDescriptionBase");
 }

 public virtual object LoadImplementationInstance()
 {
  if (!IsImplementationInstance)
  {
   throw new InvalidOperationException("Current description is not IsImplementationInstance");
  }

  return LoadImplementationInstanceCore();
 }

 protected virtual Type LoadImplementationTypeCore()
 {
  throw new InvalidOperationException("Someone lied in ServiceDescriptionBase");
 }

 public virtual Type LoadImplementationType()
 {
  if (!IsImplementationType)
  {
   throw new InvalidOperationException("Current description is not IsImplementationType");
  }

  return LoadImplementationTypeCore();
 }

 protected virtual (IEnumerable<IServiceDescription>, Type elementContractType, Type? requestedCollectionType) GetEnumerableDescriptionCore()
 {
  throw new InvalidOperationException("Someone lied in ServiceDescriptionBase");
 }

 public virtual (IEnumerable<IServiceDescription>, Type elementContractType, Type? requestedCollectionType) GetEnumerableDescription()
 {
  if (!IsEnumerableType)
  {
   throw new InvalidOperationException("Current description is not IsEnumerableType");
  }

  return GetEnumerableDescriptionCore();
 }
}