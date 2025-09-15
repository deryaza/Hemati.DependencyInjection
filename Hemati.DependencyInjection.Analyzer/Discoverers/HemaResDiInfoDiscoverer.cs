// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;
using Microsoft.CodeAnalysis;

namespace Hemati.DependencyInjection.Analyzer.Discoverers;

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
        if (attrClass.BaseType is null || !attrClass.BaseType.NamedLike("Hemati.DependencyInjection", "ServiceExportAttribute"))
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
            throw new InvalidOperationException($"Can't find lifetime of service with export attribute {attrClass.Name}.");
        }

        implType.ImplementationType = symbol.ToAssemblyQualifiedName();
        implType.ContractType = typeContract ?? implType.ImplementationType;
        implType.CreationPolicy = lifetime;
        return true;
    }
}
