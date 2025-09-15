// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Hemati.DependencyInjection.Implementation.ServiceDescriptions;

public class AspNetServiceDescriptorBasedServiceDescription : ServiceDescriptionBase
{
 private readonly ServiceDescriptor _serviceDescriptor;

 public AspNetServiceDescriptorBasedServiceDescription(ServiceDescriptor serviceDescriptor)
 {
  _serviceDescriptor = serviceDescriptor;
 }

 public override BaseServiceKey GetBaseServiceKey() => new(LoadServiceContract(), null);

 public override Type LoadServiceContract() => _serviceDescriptor.ServiceType;

 public override HbServiceLifetime GetServiceScope() =>
  _serviceDescriptor.Lifetime switch
  {
   ServiceLifetime.Singleton => HbServiceLifetime.Singleton,
   ServiceLifetime.Scoped => HbServiceLifetime.Scoped,
   ServiceLifetime.Transient => HbServiceLifetime.Transient,
   (ServiceLifetime)3 => HbServiceLifetime.ConnectionWide,
   (ServiceLifetime)4 => HbServiceLifetime.ConnectionCache,
   _ => throw new ArgumentOutOfRangeException()
  };

 public override bool IsPromiseToAddServiceDescriptor => _serviceDescriptor is PromiseToAddServiceDescriptor;

 public override bool IsImplementationFactory => _serviceDescriptor.ImplementationFactory is not null;

 public override bool IsImplementationInstance => _serviceDescriptor.ImplementationInstance is not null;

 public override bool IsImplementationType => _serviceDescriptor.ImplementationType is not null;

 protected override Func<IServiceProvider, object?> LoadFactoryCore() => _serviceDescriptor.ImplementationFactory!;

 protected override object LoadImplementationInstanceCore() => _serviceDescriptor.ImplementationInstance!;

 protected override Type LoadImplementationTypeCore() => _serviceDescriptor.ImplementationType!;
}