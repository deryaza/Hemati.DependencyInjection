// SPDX-License-Identifier: LGPL-3.0-only

using Microsoft.CodeAnalysis;

namespace Hemati.DependencyInjection.Analyzer;

public static class Extensions
{
    private static readonly SymbolDisplayFormat sdf = new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    public static bool NamedLike(this INamedTypeSymbol typeSymbol, string ns, string type)
    {
        return typeSymbol.ContainingNamespace.ToDisplayString() == ns && typeSymbol.Name == type;
    }

    private static string GetPossiblyGenericName(ITypeSymbol symbol)
    {
        if (symbol is not INamedTypeSymbol { IsGenericType: true } s)
        {
            return symbol.ToDisplayString(sdf);
        }

        int typeArgumentsLength = s.TypeArguments.Length;
        string argumentsFormatted = string.Join(",", s.TypeArguments.Select(x => $"[{x.ToAssemblyQualifiedName()}]"));
        string name = $"{symbol.ToDisplayString(sdf)}`{typeArgumentsLength}[{argumentsFormatted}]";
        return name;
    }
    public static string ToAssemblyQualifiedName(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol.ContainingType is null)
        {
            return $"{GetPossiblyGenericName(typeSymbol)}, {typeSymbol.ContainingAssembly.Name}";
        }

        string name = $"{typeSymbol.ContainingNamespace.ToDisplayString()}.";
        ITypeSymbol? containingType = typeSymbol;
        while ((containingType = containingType.ContainingType) != null)
        {
            name += $"{containingType.Name}+";
        }
        name += typeSymbol.Name;
        return $"{name}, {typeSymbol.ContainingAssembly.Name}";
    }
}
