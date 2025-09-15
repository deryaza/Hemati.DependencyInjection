// SPDX-License-Identifier: LGPL-3.0-only

using System.Reflection;
using Hemati.DependencyInjection.Implementation.Core;

namespace Hemati.DependencyInjection.Implementation.Parameters;

public class ImplementationTypeParameter : Parameter
{
 public ImplementationTypeParameter(Type service, HbServiceLifetime scope, ConstructorInfo constructor, ImplementationInformation impInfo, Parameter[] parameters)
  : base(service, scope, impInfo, parameters)
 {
  Constructor = constructor;
 }

 public ConstructorInfo Constructor { get; }

 public override bool Equals(Parameter other)
 {
  return other is ImplementationTypeParameter tp &&
      tp.Service == Service &&
      tp.Scope == Scope &&
      tp.Constructor.DeclaringType == Constructor.DeclaringType;
 }
}