// SPDX-License-Identifier: LGPL-3.0-only

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Hemati.DependencyInjection.Implementation.Core;

namespace Hemati.DependencyInjection.Implementation.ServiceDescriptions;

public class PrecomputedServiceDescription(PrecomputedServiceDescriptionData serviceDescriptionData) : ServiceDescriptionBase(serviceDescriptionData.KeyLikeContract)
{
 private void ThrowIfNull([NotNull] Type? item, string message)
 {
  if (item is not null) return;
  throw new InvalidOperationException(message);
 }

 private Type LoadTypeFromNameOrThrow(string type, string itemType)
 {
  Type? serviceContract = Type.GetType(type);
  ThrowIfNull(serviceContract, $"Failed to load {itemType} type '{type}'.");
  return serviceContract;
 }

 private T IterateAllServiceContractTypePossibilities<T>(Func<string, T> ifSpecified, Func<string, T> ifCustomAttributeSpecified, Func<PrecomputedServiceDescriptionData, T> ifNotSpecified)
 {
  return serviceDescriptionData switch
  {
   { ContractType: string contract } => ifSpecified(contract),
   { CustomAttributeType: string customAttribute } => ifCustomAttributeSpecified(customAttribute),
   { } data => ifNotSpecified(data)
  };
 }

 private (Type Type, ExportAttribute Instance) LoadCustomAttribute(string customAttribute)
 {
  Type type = LoadTypeFromNameOrThrow(customAttribute, "custom attribute");

  object?[]? args = null;
  if (serviceDescriptionData.CustomAttributeCtorArgsCreator is { } argsCreator)
  {
   args = argsCreator();
  }

  object? instance = Activator.CreateInstance(type, args);
  if (instance is not ExportAttribute ea)
  {
   throw new InvalidOperationException($"Attribute {customAttribute} was not of type ExportAttribute.");
  }

  return (type, ea);
 }

 public override Dictionary<string, object?> GetMetadata()
 {
  Dictionary<string, object?> res = serviceDescriptionData.Metadata ?? new();
  if (serviceDescriptionData.CustomAttributeType is { } attrTypeName)
  {
   (Type type, ExportAttribute instance) = LoadCustomAttribute(attrTypeName);
   foreach (PropertyInfo propertyInfo in type.GetProperties())
   {
    if (propertyInfo.DeclaringType != type)
    {
     continue;
    }

    res[propertyInfo.Name] = propertyInfo.GetValue(instance);
   }
  }

  return res;
 }

 public override BaseServiceKey GetBaseServiceKey()
 {
  return IterateAllServiceContractTypePossibilities<BaseServiceKey>(
   ifSpecified: contract => new(contract, StringContract),
   ifCustomAttributeSpecified: customAttr => LoadCustomAttribute(customAttr) is { Instance.ContractType: { } type }
    ? new(type, StringContract)
    : new(serviceDescriptionData.ImplementationType, StringContract),
   ifNotSpecified: data => new(data.ImplementationType, StringContract)
  );
 }

 public override Type LoadServiceContract()
 {
  return IterateAllServiceContractTypePossibilities<Type>(
   ifSpecified: contract => LoadTypeFromNameOrThrow(contract, "type contract"),
   ifCustomAttributeSpecified: customAttr => LoadCustomAttribute(customAttr).Instance.ContractType ?? LoadTypeFromNameOrThrow(serviceDescriptionData.ImplementationType, "type contract"),
   ifNotSpecified: data => LoadTypeFromNameOrThrow(data.ImplementationType, "type contract")
  );
 }

 public override HbServiceLifetime GetServiceScope() => serviceDescriptionData.CreationPolicy;

 public override string? Tag => serviceDescriptionData.Tag;

 public override bool IsImplementationType => true;

 protected override Type LoadImplementationTypeCore() => LoadTypeFromNameOrThrow(serviceDescriptionData.ImplementationType, "implementation type");
}