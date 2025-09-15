// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;

namespace Hemati.DependencyInjection;

public struct PrecomputedServiceDescriptionData(
 string implementationType,
 string? keyLikeContract,
 string? contractType,
 HbServiceLifetime creationPolicy,
 string? customAttributeType,
 Func<object[]?>? customAttributeCtorArgsCreator,
 Dictionary<string, object?>? metadata,
 string tag)
{
 public readonly string ImplementationType = implementationType;
 public readonly string? KeyLikeContract = keyLikeContract;
 public readonly string? ContractType = contractType;
 public readonly HbServiceLifetime CreationPolicy = creationPolicy;
 public readonly string? CustomAttributeType = customAttributeType;
 public readonly Func<object[]?>? CustomAttributeCtorArgsCreator = customAttributeCtorArgsCreator;
 public readonly Dictionary<string, object?>? Metadata = metadata;
 public readonly string Tag = tag;
}