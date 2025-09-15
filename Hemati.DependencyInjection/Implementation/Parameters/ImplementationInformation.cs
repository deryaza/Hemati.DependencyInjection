// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.ServiceDescriptions;

namespace Hemati.DependencyInjection.Implementation.Parameters;

public class ImplementationInformation
{
 private readonly IServiceDescription? serviceDescription;
 private readonly ServicesDescriptor? servicesDescriptor;

 public ImplementationInformation(IServiceDescription serviceDescription, ServicesDescriptor servicesDescriptor)
 {
  this.serviceDescription = serviceDescription;
  this.servicesDescriptor = servicesDescriptor;
 }

 public static ImplementationInformation Default => new ImplementationInformation(null, null);

 public bool TryGetBaseServiceKey(out BaseServiceKey serviceKey)
 {
  if (serviceDescription is not { } sd)
  {
   serviceKey = default;
   return false;
  }

  serviceKey = sd.GetBaseServiceKey();
  return true;
 }

 public int GetImplementationNumber()
 {
  if (servicesDescriptor is null || serviceDescription is null)
   return 0;

  return servicesDescriptor.GetImplementationNumber(serviceDescription);
 }
}