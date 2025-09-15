// SPDX-License-Identifier: LGPL-3.0-only

using System.Diagnostics;

namespace Hemati.DependencyInjection.Analyzer;

using System;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Hemati.DependencyInjection.Implementation.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EmitAllServicesInCompilation : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        new("HMDI9998", "Analyzer that precomputes service exports", "If you see this, you got a problem", "Design", DiagnosticSeverity.Warning, isEnabledByDefault: true)
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(OnCompilation);
    }

    private void OnCompilation(CompilationAnalysisContext context)
    {
        if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property._HematiIntermediateDir", out string? val))
        {
            return;
        }

        INamespaceSymbol root = context.Compilation.Assembly.GlobalNamespace;
        NamespaceVisitor visitor = new(context.Compilation);
        visitor.Visit(root);

        IExporter exporter = new CSharpCodeExporter();
        exporter.ExportPath = val;
    }
}

file class NamespaceVisitor : SymbolVisitor
{
    public readonly Dictionary<string, OneOf> AllTypesThatExportSomething = new();
    private DiInfoDiscovererCtx _ctx;

    public NamespaceVisitor(Compilation compilation)
    {
        _ctx = new(compilation);
    }

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        foreach (INamespaceOrTypeSymbol namespaceOrType in symbol.GetMembers())
        {
            namespaceOrType.Accept(this);
        }
    }

    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
        OneOf oneOfdiInfo = DiInfoDiscoverer.DiscoverDiInfo(symbol, ref _ctx);
        if (oneOfdiInfo is { Single.IsCorrect: true } || oneOfdiInfo is { Multiple.Length: > 0 })
        {
            int duplicates = 1;
            string key = symbol.Name;
            while (AllTypesThatExportSomething.ContainsKey(key))
            {
                key = $"{symbol.Name}_{duplicates++}";
            }

            AllTypesThatExportSomething.Add(key, oneOfdiInfo);
        }

        foreach (ISymbol member in symbol.GetTypeMembers())
        {
            member.Accept(this);
        }
    }
}

public struct DiInfoDiscovererCtx(Compilation compilation)
{
    public readonly Compilation Compilation = compilation;
}
