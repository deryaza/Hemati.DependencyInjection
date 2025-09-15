// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;

namespace Hemati.DependencyInjection.Implementation.Parameters;

public class FactoryParameter : Parameter
{
 public FactoryParameter(Func<IServiceProvider, object?> factory, ImplementationInformation implementationInformation, Type service, HbServiceLifetime scope)
  : base(service, scope, implementationInformation, Array.Empty<Parameter>())
 {
  Factory = factory;
 }

 public Func<IServiceProvider, object?> Factory { get; }

 public override bool Equals(Parameter other)
 {
  return other is FactoryParameter fp &&
      fp.Service == Service &&
      fp.Scope == Scope &&
      fp.Factory == Factory;
 }
}