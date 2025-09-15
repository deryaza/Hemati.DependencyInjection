// SPDX-License-Identifier: LGPL-3.0-only

namespace Hemati.DependencyInjection.Implementation;

public struct FindServiceRequest(Type serviceType)
{
 public readonly Type ServiceType = serviceType;
 public bool IsImportManyRequest;
 public string? StringContract;

 public BaseServiceKey ToBaseServiceKey()
 {
  return new(ServiceType, StringContract);
 }
}