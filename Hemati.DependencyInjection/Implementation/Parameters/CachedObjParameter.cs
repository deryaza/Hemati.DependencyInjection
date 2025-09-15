// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;

namespace Hemati.DependencyInjection.Implementation.Parameters;

public class CachedObjParameter : Parameter
{
 public CachedObjParameter(Type service, ImplementationInformation implementationInformation)
  : base(service, HbServiceLifetime.ConnectionCache, implementationInformation, Array.Empty<Parameter>())
 {
 }

 public override bool Equals(Parameter other)
 {
  return other is ConstantParameter cp && cp.Service == Service;
 }
}