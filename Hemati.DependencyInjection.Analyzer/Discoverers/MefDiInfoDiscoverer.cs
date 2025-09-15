// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Hemati.DependencyInjection.Analyzer.Discoverers;

public static class MefDiInfoDiscoverer
{
    public static bool CheckIfExportAttribute(INamedTypeSymbol attrClass, AttributeData attributeData, ITypeSymbol typeSymbol, ref TypeDiInfo typeDiInfo)
    {
        if (!attrClass.NamedLike("System.ComponentModel.Composition", "ExportAttribute"))
        {
            return false;
        }

        string? typeContract = null;
        string? stringContract = null;

        foreach (TypedConstant ctorArg in attributeData.ConstructorArguments)
        {
            if (ctorArg.Kind == TypedConstantKind.Type)
            {
                INamedTypeSymbol typeofType = (INamedTypeSymbol)ctorArg.Value!;
                typeContract = typeofType.ToAssemblyQualifiedName();
            }
            else
            {
                stringContract = ctorArg.ToCSharpString();
            }
        }

        typeDiInfo.ImplementationType = typeSymbol.ToAssemblyQualifiedName();
        typeDiInfo.ContractType = typeContract ?? typeDiInfo.ImplementationType;
        typeDiInfo.KeyLikeContract = stringContract;
        return true;
    }

    public static bool AddCustomAttribute(INamedTypeSymbol attrClass, AttributeData attributeData, ITypeSymbol typeSymbol, ref TypeDiInfo typeDiInfo)
    {
        if (attrClass.BaseType is not { } bt || !bt.NamedLike("System.ComponentModel.Composition", "ExportAttribute"))
        {
            return false;
        }

        typeDiInfo.ImplementationType = typeSymbol.ToAssemblyQualifiedName();
        typeDiInfo.CustomAttributeType = attrClass.ToAssemblyQualifiedName();

        if (attributeData.ConstructorArguments.Length > 0)
        {
            typeDiInfo.CustomAttributeArgs = attributeData.ConstructorArguments.Select(DiInfoDiscoverer.ToMetadataExpression).ToArray();
        }

        if (attributeData.NamedArguments.Length > 0)
        {
            typeDiInfo.CustomAttributeParameterAssigmentsArgs ??= new();
            foreach (var attributeDataNamedArgument in attributeData.NamedArguments)
            {
                typeDiInfo.CustomAttributeParameterAssigmentsArgs[attributeDataNamedArgument.Key] = attributeDataNamedArgument.Value.ToMetadataExpression();
            }
        }

        return true;
    }

    public static void AddCreationPolicy(INamedTypeSymbol attrClass, AttributeData attrData, ref TypeDiInfo info)
    {
        if (!attrClass.NamedLike("System.ComponentModel.Composition", "PartCreationPolicyAttribute"))
        {
            return;
        }

        info.CreationPolicy = attrData.ConstructorArguments[0].Value switch
        {
            0 => HbServiceLifetime.Singleton,
            1 => HbServiceLifetime.Singleton,
            2 => HbServiceLifetime.Transient,
            _ => HbServiceLifetime.Singleton,
        };
    }

    public static void AddMetadata(INamedTypeSymbol attrClass, AttributeData attrData, ref TypeDiInfo info)
    {
        if (!attrClass.NamedLike("System.ComponentModel.Composition", "ExportMetadataAttribute"))
        {
            return;
        }

        string key = (string)attrData.ConstructorArguments[0].Value!;
        
        TypedConstant constant = attrData.ConstructorArguments[1];
        MetadataExpression metadataValue = DiInfoDiscoverer.ToMetadataExpression(constant);

        info.Metadata ??= [];
        info.Metadata[key] = metadataValue;
    }
}

