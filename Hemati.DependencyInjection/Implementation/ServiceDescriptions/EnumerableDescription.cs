// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;

namespace Hemati.DependencyInjection.Implementation.ServiceDescriptions;

public class EnumerableDescription(
 Type constructedEnumerableType,
 Type elementContractType,
 IEnumerable<IServiceDescription> elementDescriptions,
 Type? requestedCollectionType) : ServiceDescriptionBase
{
 public Type? RequestedCollectionType { get; } = requestedCollectionType;

 public override bool IsEnumerableType => true;

 public override BaseServiceKey GetBaseServiceKey() => new(constructedEnumerableType, null);

 public override Type LoadServiceContract() => constructedEnumerableType;

 public override HbServiceLifetime GetServiceScope() => HbServiceLifetime.Transient;

 protected override (IEnumerable<IServiceDescription>, Type elementContractType, Type? requestedCollectionType) GetEnumerableDescriptionCore()
 {
  return (elementDescriptions, elementContractType, RequestedCollectionType);
 }
}