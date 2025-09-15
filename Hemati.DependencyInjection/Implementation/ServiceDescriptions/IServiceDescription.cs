// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;

namespace Hemati.DependencyInjection.Implementation.ServiceDescriptions;

public interface IServiceDescription
{
 string? Tag { get; }
 bool HasMetadata { get; }
 bool IsPromiseToAddServiceDescriptor { get; }
 bool IsImplementationFactory { get; }
 bool IsImplementationInstance { get; }
 bool IsImplementationType { get; }
 bool IsEnumerableType { get; }
 Dictionary<string, object?> GetMetadata();
 BaseServiceKey GetBaseServiceKey();
 bool SatisfiesStringContract(string? contract);
 bool IsSameContractType(Type type);
 Type LoadServiceContract();
 HbServiceLifetime GetServiceScope();
 Func<IServiceProvider, object?> LoadFactory();
 object LoadImplementationInstance();
 Type LoadImplementationType();
 (IEnumerable<IServiceDescription>, Type elementContractType, Type? requestedCollectionType) GetEnumerableDescription();
}