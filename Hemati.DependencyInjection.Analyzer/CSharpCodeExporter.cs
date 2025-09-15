// SPDX-License-Identifier: LGPL-3.0-only

using System.CodeDom.Compiler;
using System.Text;

namespace Hemati.DependencyInjection.Analyzer;

public class CSharpCodeExporter : IExporter
{
    public string ExportPath { get; set; }

    public void Export(Dictionary<string, OneOf> allTypesThatExportSomething)
    {
        List<string> filesInCurrentCompilation = new(allTypesThatExportSomething.Count);

        StringBuilder sb = new();
        IndentedTextWriter indentedTextWriter = new(new StringWriter(sb));
        foreach (var kvp in allTypesThatExportSomething)
        {
            string baseClassName = kvp.Key;
            OneOf oneOfTypeDiInfo = kvp.Value;

            string className = baseClassName + "DiInfo";
            string fileName = className + ".cs";
            filesInCurrentCompilation.Add(fileName);

            GenerateCode(indentedTextWriter, className, oneOfTypeDiInfo);

            string fullFilePath = Path.Combine(ExportPath, fileName);
            File.WriteAllText(fullFilePath, sb.ToString());
            sb.Clear();
        }

        // generate master type
        {
            string fileName = "AssemblyMasterDiInfo.cs";
            string fullFilePath = Path.Combine(ExportPath, fileName);

            GenerateCommonFile(indentedTextWriter, allTypesThatExportSomething);
            File.WriteAllText(fullFilePath, sb.ToString());
            filesInCurrentCompilation.Add(fileName);
            sb.Clear();
        }

        File.WriteAllLines(Path.Combine(ExportPath, "all-compilation-files.txt"), filesInCurrentCompilation);
    }

    static void GenerateMethod(IndentedTextWriter sb, TypeDiInfo ti, string methodName)
    {
        sb.Write("public static PrecomputedServiceDescriptionData ");
        sb.Write(methodName);
        sb.WriteLine("()");
        sb.WriteLine("{");
        sb.Indent++;
        sb.WriteLine("return new(");
        sb.Indent++;
        sb.Write("implementationType: ");
        WriteQuoted(sb, ti.ImplementationType);
        sb.WriteLine(",");
        sb.Write("keyLikeContract: ");
        WriteDefaultIfNull(sb, ti.KeyLikeContract);
        sb.WriteLine(",");
        sb.Write("contractType: ");
        WriteDefaultIfNull(sb, ti.ContractType, WriteQuoted);
        sb.WriteLine(",");
        sb.Write("creationPolicy: HbServiceLifetime.");
        sb.Write(ti.CreationPolicy.ToString());
        sb.WriteLine(",");
        sb.Write("customAttributeType: ");
        WriteDefaultIfNull(sb, ti.CustomAttributeType, WriteQuoted);
        sb.WriteLine(",");
        if (ti.Metadata is null)
        {
            sb.WriteLine("metadata: null,");
        }
        else
        {
            sb.WriteLine("metadata: new()");
            sb.WriteLine("{");
            sb.Indent++;

            foreach (var kv in ti.Metadata)
            {
                string key = kv.Key;
                var val = kv.Value;
                sb.Write('[');
                WriteQuoted(sb, key);
                sb.Write("] = ");
                WriteExpression(sb, val);
                sb.WriteLine(',');
            }

            sb.Indent--;
            sb.WriteLine("},");
        }

        if (ti.CustomAttributeArgs is null)
        {
            sb.WriteLine("customAttributeCtorArgsCreator: null,");
        }
        else
        {
            sb.WriteLine("customAttributeCtorArgsCreator: () =>");
            sb.WriteLine("{");
            sb.Indent++;
            sb.WriteLine("return new object[]");
            sb.WriteLine("{");
            sb.Indent++;
            foreach (var arg in ti.CustomAttributeArgs)
            {
                WriteExpression(sb, arg);
                sb.WriteLine(',');
            }
            sb.Indent--;
            sb.WriteLine("};");
            sb.Indent--;
            sb.WriteLine("},");
        }

        sb.Write($"tag: ");
        WriteQuoted(sb, ti.ImplementationType);

        sb.Indent--;
        sb.WriteLine(");");
        sb.Indent--;
        sb.WriteLine("}");
    }

    static void GenerateCode(IndentedTextWriter sb, string className, OneOf ti)
    {
        sb.WriteLine("using System;");
        sb.WriteLine("using Hemati.DependencyInjection;");
        sb.WriteLine("using Hemati.DependencyInjection.Implementation.Core;");
        sb.WriteLine("namespace Hemati.PreGeneratedInfo;");
        sb.Write("internal static class ");
        sb.WriteLine(className);
        sb.WriteLine("{");
        sb.Indent++;

        if (ti.Single is { } single)
        {
            GenerateMethod(sb, single, "GetExportDescriptionForThisType");
        }
        else if (ti.Multiple is { } multiple)
        {
            for (int index = 0; index < multiple.Length; index++)
            {
                TypeDiInfo oneOfMultiple = multiple[index];
                GenerateMethod(sb, oneOfMultiple, $"GetExportDescriptionForThisType{index}");
            }
        }

        sb.Indent--;
        sb.WriteLine("}");
    }

    static void WriteExpression(IndentedTextWriter sb, MetadataExpression metadataExpression)
    {
        switch (metadataExpression.ExpressionType)
        {
            case MetadataExpressionType.TypeOf:
                sb.Write("Type.GetType(");
                WriteQuoted(sb, metadataExpression.ExpressionToEmit);
                sb.Write(")");
                break;
            case MetadataExpressionType.Primitive:
                if (metadataExpression.TypeExpression == "string")
                {
                    WriteDefaultIfNull(sb, metadataExpression.ExpressionToEmit, WriteQuoted);
                }
                else
                {
                    // I just assume that decimals/doubles/longs are allright
                    sb.Write(metadataExpression.ExpressionToEmit);
                }

                break;
            case MetadataExpressionType.Enum:
                sb.Write("Enum.ToObject(");
                sb.Write("Type.GetType(");
                WriteQuoted(sb, metadataExpression.TypeExpression);
                sb.Write("), ");
                sb.Write(metadataExpression.ExpressionToEmit);
                sb.Write(")");
                break;
        }
    }

    static void WriteDefaultIfNull(IndentedTextWriter sb, string? text, Action<IndentedTextWriter, string>? formatter = null)
    {
        if (text is null)
        {
            sb.Write("default");
        }
        else if (formatter is not null)
        {
            formatter(sb, text);
        }
        else
        {
            sb.Write(text);
        }
    }

    static void WriteQuoted(IndentedTextWriter sb, string? text)
    {
        sb.Write('\"');
        sb.Write(text);
        sb.Write('\"');
    }

    static void GenerateCommonFile(IndentedTextWriter sb, Dictionary<string, OneOf> allTypesThatExportSomething)
    {
        sb.WriteLine("using System;");
        sb.WriteLine("using Hemati.DependencyInjection;");
        sb.WriteLine("using Hemati.DependencyInjection.Implementation.Core;");
        sb.WriteLine("namespace Hemati.PreGeneratedInfo;");
        sb.WriteLine("public static class DllMasterDiInfo");
        sb.WriteLine("{");
        sb.Indent++;
        sb.WriteLine("public static PrecomputedServiceDescriptionData[] GetExportDescriptions()");
        sb.WriteLine("{");
        sb.Indent++;
        sb.WriteLine("return");
        sb.WriteLine("[");
        sb.Indent++;
        foreach (var kv in allTypesThatExportSomething)
        {
            string className = kv.Key + "DiInfo";
            if (kv.Value.Single is not null)
            {
                sb.Write(className);
                sb.WriteLine(".GetExportDescriptionForThisType(),");
            }

            if (kv.Value.Multiple is { Length: var count })
            {
                for (int i = 0; i < count; i++)
                {
                    sb.Write(className);
                    sb.Write(".GetExportDescriptionForThisType");
                    sb.Write(i);
                    sb.WriteLine("(),");
                }
            }
        }

        sb.Indent--;
        sb.WriteLine("];");
        sb.Indent--;
        sb.WriteLine("}");
        sb.Indent--;
        sb.WriteLine("}");
    }
}
