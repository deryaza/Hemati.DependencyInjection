// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;

namespace Hemati.DependencyInjection.Implementation.Parameters;

public class UnknownParameter : Parameter
{
 public UnknownParameter(Type service, HbServiceLifetime scope)
  : base(service, scope, ImplementationInformation.Default, Array.Empty<Parameter>())
 {
 }

 public override bool Equals(Parameter other)
 {
  return other is UnknownParameter up &&
      up.Service == Service &&
      up.Scope == Scope;
 }
}