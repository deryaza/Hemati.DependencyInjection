// SPDX-License-Identifier: LGPL-3.0-only

namespace Hemati.DependencyInjection.Implementation;

public struct BaseServiceKey : IEquatable<BaseServiceKey>
{
 private readonly string _typeName;
 private readonly string? _stringContract;

 public BaseServiceKey(Type type, string? stringContract)
  : this(ConvertTypeToString(type), stringContract)
 {
 }

 public BaseServiceKey(string typeName, string? stringContract)
 {
  _typeName = typeName;
  _stringContract = stringContract;
 }

 public string TypeName => _typeName;
 public string? StringContract => _stringContract;

 public override string ToString() =>
  (_typeName, _stringContract) switch
  {
   (null, _) => "<INVALID KEY>",
   ({ } type, null) => type,
   ({ } type, { } contract) => $"{contract} / {type}"
  };

 private static string ConvertTypeToString(Type type)
 {
  if (!type.IsGenericType)
   return $"{type.FullName}, {type.Assembly.GetName().Name}";

  string argumentsFormated = string.Join(", ", type.GenericTypeArguments.Select(x => $"{ConvertTypeToString(x)}"));
  string name = $"{type.Namespace}.{type.Name}[{argumentsFormated}], {type.Assembly.GetName().Name}";
  return name;
 }

 public bool Equals(BaseServiceKey other) =>
  _typeName == other._typeName && _stringContract == other._stringContract;

 public override bool Equals(object? obj) =>
  obj is BaseServiceKey other && Equals(other);

 public override int GetHashCode() =>
  HashCode.Combine(_typeName, _stringContract);

 public static bool operator ==(BaseServiceKey left, BaseServiceKey right) =>
  left.Equals(right);

 public static bool operator !=(BaseServiceKey left, BaseServiceKey right) =>
  !left.Equals(right);
}