// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;

using Microsoft.CodeAnalysis;

namespace Hemati.DependencyInjection.Analyzer.Discoverers;

using Microsoft.CodeAnalysis.CSharp;

public static class HemaResDiInfoDiscoverer
{
    private static readonly Dictionary<string, HbServiceLifetime> AttributesToLifetimes = new()
    {
        ["ConnectionWideImplementationOfAttribute"] = HbServiceLifetime.ConnectionWide,
        ["ScopedImplementationOfAttribute"] = HbServiceLifetime.Scoped,
        ["SingletonImplementationOfAttribute"] = HbServiceLifetime.Singleton,
        ["TransientImplementationOfAttribute"] = HbServiceLifetime.Transient,
    };

    public static bool CheckIfExportAttribute(INamedTypeSymbol attrClass, AttributeData attributeData, ITypeSymbol symbol, ref TypeDiInfo implType)
    {
        if (attrClass.BaseType is null || !attrClass.BaseType.NamedLike("Halfblood.Framework.ComponentSystem.Server", "ServiceExportAttribute"))
        {
            return false;
        }

        string? typeContract = null;

        foreach (TypedConstant ctorArg in attributeData.ConstructorArguments)
        {
            if (ctorArg.Kind != TypedConstantKind.Type)
            {
                continue;
            }

            INamedTypeSymbol typeofType = (INamedTypeSymbol)ctorArg.Value!;
            typeContract = typeofType.ToAssemblyQualifiedName();
            break;
        }

        if (!AttributesToLifetimes.TryGetValue(attrClass.Name, out var lifetime))
        {
            if (TryGetLifetimeFromCtorArg(attributeData) is not HbServiceLifetime lifetimeFromArgs)
            {
                throw new InvalidOperationException($"Can't find life time of service with export attribute {attrClass.Name}.");
            }

            if (attributeData.AttributeConstructor?.Parameters.FirstOrDefault(x => x.Name == "key") is { } keyParam)
            {
                implType.KeyLikeContract = attributeData.ConstructorArguments[keyParam.Ordinal].ToCSharpString();
            }

            lifetime = lifetimeFromArgs;
        }

        implType.ImplementationType = symbol.ToAssemblyQualifiedName();
        implType.ContractType = typeContract ?? implType.ImplementationType;
        implType.CreationPolicy = lifetime;
        return true;
    }

    private static HbServiceLifetime? TryGetLifetimeFromCtorArg(AttributeData attributeData)
    {
        foreach (TypedConstant ctorArg in attributeData.ConstructorArguments)
        {
            if (ctorArg is not { Kind: TypedConstantKind.Enum, Type: INamedTypeSymbol namedType })
            {
                continue;
            }

            foreach (ISymbol enumMember in namedType.GetMembers())
            {
                if (enumMember is IFieldSymbol { IsConst: true, HasConstantValue: true } symbol
                    && Equals(symbol.ConstantValue, ctorArg.Value))
                {
                    switch (enumMember.Name)
                    {
                        case "Singleton": return HbServiceLifetime.Singleton;
                        case "Scoped": return HbServiceLifetime.Scoped;
                        case "Transient": return HbServiceLifetime.Transient;
                        case "ConnectionWide": return HbServiceLifetime.ConnectionWide;
                        case "ConnectionCache": return HbServiceLifetime.ConnectionCache;
                    }
                }
            }
        }

        return null;
    }
}