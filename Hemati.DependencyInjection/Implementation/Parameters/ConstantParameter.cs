// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;

namespace Hemati.DependencyInjection.Implementation.Parameters;

public class ConstantParameter : Parameter
{
 public ConstantParameter(object impl, Type service, ImplementationInformation implementationInformation, HbServiceLifetime scope)
  : base(service, scope, implementationInformation, Array.Empty<Parameter>())
 {
  Impl = impl;
 }

 public object Impl { get; }

 public override bool Equals(Parameter other)
 {
  return other is ConstantParameter cp &&
      cp.Service == Service &&
      cp.Impl == Impl;
 }
}