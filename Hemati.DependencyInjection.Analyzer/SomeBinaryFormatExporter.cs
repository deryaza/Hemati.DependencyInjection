// SPDX-License-Identifier: LGPL-3.0-only

using System.Diagnostics;
using Hemati.DependencyInjection.Serialization;
using Microsoft.CodeAnalysis;

namespace Hemati.DependencyInjection.Analyzer;

internal sealed class SomeBinaryFormatExporter : IExporter
{
    public string? ExportPath { get; set; }

    public void Export(Dictionary<string, OneOf> allTypesThatExportSomething)
    {
        if (ExportPath is null)
        {
            throw new InvalidOperationException($"{nameof(ExportPath)} must be set.");
        }

        TypeDiInfo[] allTypes = allTypesThatExportSomething.SelectMany(x =>
        {
            OneOf argValue = x.Value;
            return argValue.Single != null ? [argValue.Single.Value] : argValue.Multiple;
        }).ToArray();

        /*
         *  Ideas:
         *  - implement kind of a lookup table at start of an entry maybe?
         */

        using BinaryWriter binaryWriter = new(File.OpenWrite(Path.Combine(ExportPath, "service-descriptions.precomputed")));
        DoWrite(binaryWriter, allTypes);
        binaryWriter.Flush();
    }

    private static void DoWrite(BinaryWriter w, TypeDiInfo[] types)
    {
        w.Write(types.Length);
        foreach (TypeDiInfo typeDiInfo in types)
        {
            WriteOne(w, in typeDiInfo);
        }
    }

    private static void WriteOne(BinaryWriter w, in TypeDiInfo typeDiInfo)
    {
        Debug.Assert(typeDiInfo.ImplementationType != null);
        w.Write(typeDiInfo.ImplementationType!);
        w.WriteNullableString(typeDiInfo.KeyLikeContract);
        w.WriteNullableString(typeDiInfo.ContractType);
        w.Write((byte)typeDiInfo.CreationPolicy);
        w.WriteNullableString(typeDiInfo.CustomAttributeType);
        w.WriteManyNullable(typeDiInfo.CustomAttributeArgs);
        w.WriteDictionaryNullable(typeDiInfo.Metadata);
    }
}

file static class BinaryExtensions
{
    extension(BinaryWriter w)
    {
        private void WritePrimitive(object val)
        {
            switch (val)
            {
                case bool value:
                {
                    w.Write((byte)BinaryPrimitiveType.T_Bool);
                    w.Write(value);
                    break;
                }
                case byte value:
                {
                    w.Write((byte)BinaryPrimitiveType.T_Byte);
                    w.Write(value);
                    break;
                }
                case char ch:
                {
                    w.Write((byte)BinaryPrimitiveType.T_Char);
                    w.Write(ch);
                    break;
                }
                case decimal value:
                {
                    w.Write((byte)BinaryPrimitiveType.T_Decimal);
                    w.Write(value);
                    break;
                }
                case double value:
                {
                    w.Write((byte)BinaryPrimitiveType.T_Double);
                    w.Write(value);
                    break;
                }
                case short value:
                {
                    w.Write((byte)BinaryPrimitiveType.T_Short);
                    w.Write(value);
                    break;
                }
                case int value:
                {
                    w.Write((byte)BinaryPrimitiveType.T_Int);
                    w.Write(value);
                    break;
                }
                case long value:
                {
                    w.Write((byte)BinaryPrimitiveType.T_Long);
                    w.Write(value);
                    break;
                }
                case sbyte value:
                {
                    w.Write((byte)BinaryPrimitiveType.T_Sbyte);
                    w.Write(value);
                    break;
                }
                case float value:
                {
                    w.Write((byte)BinaryPrimitiveType.T_Float);
                    w.Write(value);
                    break;
                }
                case string value:
                {
                    w.Write((byte)BinaryPrimitiveType.T_String);
                    w.Write(value);
                    break;
                }
                case ushort value:
                {
                    w.Write((byte)BinaryPrimitiveType.T_Ushort);
                    w.Write(value);
                    break;
                }
                case uint value:
                {
                    w.Write((byte)BinaryPrimitiveType.T_Uint);
                    w.Write(value);
                    break;
                }
                case ulong value:
                {
                    w.Write((byte)BinaryPrimitiveType.T_Ulong);
                    w.Write(value);
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(val), val, $"Can't serialize type {val.GetType()}");
            }
        }

        private void WriteExpression(MetadataExpression expression)
        {
            w.Write((byte)expression.ExpressionType);
            switch (expression.ExpressionType)
            {
                case MetadataExpressionType.TypeOf:
                {
                    w.Write(((ITypeSymbol)expression.Value!).ToAssemblyQualifiedName());
                    break;
                }
                case MetadataExpressionType.Primitive:
                {
                    w.WritePrimitive(expression.Value!);
                    break;
                }
                case MetadataExpressionType.Enum:
                {
                    w.Write(expression.TypeSymbol.ToAssemblyQualifiedName());
                    w.WritePrimitive(expression.Value!);
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void WriteDictionaryNullable(Dictionary<string, MetadataExpression>? metadataExpressions)
        {
            w.Write(metadataExpressions != null);
            if (metadataExpressions != null)
            {
                w.Write(metadataExpressions.Count);
                foreach (KeyValuePair<string, MetadataExpression> keyValuePair in metadataExpressions)
                {
                    w.Write(keyValuePair.Key);
                    w.WriteExpression(keyValuePair.Value);
                }
            }
        }

        public void WriteManyNullable(MetadataExpression[]? expressions)
        {
            w.Write(expressions != null);
            if (expressions != null)
            {
                w.Write(expressions.Length);
                foreach (MetadataExpression expression in expressions)
                {
                    w.WriteExpression(expression);
                }
            }
        }

        public void WriteNullableString(string? str)
        {
            w.Write(str != null);
            if (str != null)
            {
                w.Write(str);
            }
        }
    }
}