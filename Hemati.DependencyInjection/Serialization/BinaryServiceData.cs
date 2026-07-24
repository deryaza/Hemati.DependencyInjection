// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;

namespace Hemati.DependencyInjection.Serialization;

public struct BinaryServiceData
{
    public string ImplementationType;
    public string? KeyLikeContract;
    public string? ContractType;
    public HbServiceLifetime CreationPolicy;
    public string? CustomAttributeType;
    public BinaryValueData[]? CustomAttributeArgs;
    public Dictionary<string, BinaryValueData>? Metadata;
    public string? Tag;
}