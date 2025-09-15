// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;

namespace Hemati.DependencyInjection.Implementation.ServiceDescriptions;

public class ConstructedOpenGenericImplementationTypeDescription(Type contractType, Type implementationType, HbServiceLifetime serviceLifetime) : ServiceDescriptionBase
{
 public override bool IsImplementationType => true;

 public override BaseServiceKey GetBaseServiceKey() => new(contractType, null);

 public override Type LoadServiceContract() => contractType;

 public override HbServiceLifetime GetServiceScope() => serviceLifetime;

 protected override Type LoadImplementationTypeCore() => implementationType;
}