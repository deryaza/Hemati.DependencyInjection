// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;

namespace Hemati.DependencyInjection.Implementation.Parameters;

public class EnumerableParameter : Parameter
{
 public EnumerableParameter(Type serviceDesc, Type? requestedCollectionType, Type singleElementType, HbServiceLifetime scope, IEnumerable<Parameter> parameters)
  : base(serviceDesc, scope, ImplementationInformation.Default, [])
 {
  RequestedCollectionType = requestedCollectionType;
  SingleElementType = singleElementType;
  EnumerableParameters = parameters;
 }

 public Type? RequestedCollectionType { get; }
 public Type SingleElementType { get; }
 public IEnumerable<Parameter> EnumerableParameters { get; }

 public override bool Equals(Parameter other)
 {
  return other is EnumerableParameter ep &&
      ep.SingleElementType == SingleElementType &&
      ep.Scope == Scope;
 }
}