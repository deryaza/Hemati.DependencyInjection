// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Analyzer.Discoverers;
using Hemati.DependencyInjection.Implementation.Core;
using Microsoft.CodeAnalysis;

namespace Hemati.DependencyInjection.Analyzer;

public static class DiInfoDiscoverer
{
    public static OneOf DiscoverDiInfo(ITypeSymbol symbol, ref DiInfoDiscovererCtx ctx)
    {
        TypeDiInfo modelType = new()
        {
            CreationPolicy = HbServiceLifetime.Transient
        };

        // getting common props that are CreationPolicy and Metadata
        foreach (AttributeData attributeData in symbol.GetAttributes())
        {
            if (attributeData.AttributeClass is not { } attrCls)
            {
                continue;
            }

            MefDiInfoDiscoverer.AddCreationPolicy(attrCls, attributeData, ref modelType);
            MefDiInfoDiscoverer.AddMetadata(attrCls, attributeData, ref modelType);
        }

        // getting registration instances
        OneOf oneOf = default;
        List<TypeDiInfo>? multipleInfos = null;
        foreach (AttributeData attributeData in symbol.GetAttributes())
        {
            if (attributeData.AttributeClass is not { } attrClass)
            {
                continue;
            }

            TypeDiInfo implType = modelType;
            if (!MefDiInfoDiscoverer.CheckIfExportAttribute(attrClass, attributeData, symbol, ref implType)
                && !HemaResDiInfoDiscoverer.CheckIfExportAttribute(attrClass, attributeData, symbol, ref implType)
                && !MefDiInfoDiscoverer.AddCustomAttribute(attrClass, attributeData, symbol, ref implType))
            {
                continue;
            }

            if (!implType.IsCorrect)
            {
                continue;
            }

            if (oneOf is not { Single: { } single })
            {
                oneOf = new(implType, null);
                continue;
            }

            multipleInfos ??= [single];
            multipleInfos.Add(implType);
        }

        return multipleInfos is null ? oneOf : new(null, multipleInfos.ToArray());
    }

    public static MetadataExpression ToMetadataExpression(this TypedConstant constant)
    {
        return constant.Kind switch
        {
            TypedConstantKind.Primitive => new(MetadataExpressionType.Primitive, constant.Value, constant.Type!),
            TypedConstantKind.Enum => new(MetadataExpressionType.Enum, constant.Value, constant.Type!),
            TypedConstantKind.Type => new(MetadataExpressionType.TypeOf, constant.Value, constant.Type!),
            var c => throw new InvalidOperationException($"not supported constant type {c}")
        };
    }
}

public readonly struct OneOf(TypeDiInfo? single, TypeDiInfo[]? multiple)
{
    public readonly TypeDiInfo? Single = single;
    public readonly TypeDiInfo[]? Multiple = multiple;
}

public struct TypeDiInfo
{
    public string? ImplementationType;
    public string? KeyLikeContract;
    public string? ContractType;
    public HbServiceLifetime CreationPolicy;
    public string? CustomAttributeType;
    public MetadataExpression[]? CustomAttributeArgs;
#if DEBUG
    public Dictionary<string, MetadataExpression>? CustomAttributeParameterAssignmentsArgs; // NOTE: Not handled
#endif
    public Dictionary<string, MetadataExpression>? Metadata;
    public bool IsCorrect => this is { ImplementationType: not null };
}

public struct MetadataExpression(MetadataExpressionType type, object? value, ITypeSymbol typeSymbol)
{
    public MetadataExpressionType ExpressionType => type;

    public string TypeExpression => typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public string? ExpressionToEmit => value switch
    {
        true => "true",
        false => "false",
        ITypeSymbol t => t.ToAssemblyQualifiedName(),
        _ => value?.ToString(),
    };

    public object? Value => value;
    public ITypeSymbol TypeSymbol => typeSymbol;
}

public enum MetadataExpressionType
{
    TypeOf,
    Primitive,
    Enum
}