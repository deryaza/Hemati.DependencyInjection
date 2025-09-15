// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;

namespace Hemati.DependencyInjection.Implementation.Parameters;

public abstract class Parameter : IEquatable<Parameter>
{
 protected Parameter(Type service, HbServiceLifetime scope, ImplementationInformation implInfo, Parameter[] parameters)
 {
  Service = service;
  Scope = scope;
  ImplInfo = implInfo;
  Parameters = parameters;
 }

 public Type Service { get; }
 public HbServiceLifetime Scope { get; }
 public ImplementationInformation ImplInfo { get; }
 public Parameter[] Parameters { get; }

 public BaseServiceKey GetServiceKey()
 {
  return ImplInfo.TryGetBaseServiceKey(out BaseServiceKey key) ? key : new(Service, null);
 }

 public abstract bool Equals(Parameter? other);

 public override bool Equals(object? obj)
 {
  return Equals(obj as Parameter);
 }

 public override int GetHashCode()
 {
  return HashCode.Combine(Service, (int)Scope, ImplInfo, Parameters);
 }
}