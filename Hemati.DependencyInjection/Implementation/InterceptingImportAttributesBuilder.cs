// SPDX-License-Identifier: LGPL-3.0-only

using System.ComponentModel.Composition;
using System.Reflection;
using System.Reflection.Emit;
using Hemati.DependencyInjection.Implementation.Parameters;

namespace Hemati.DependencyInjection.Implementation;

public class InterceptingImportAttributesBuilder : IlServiceBuilder
{
    private static readonly MethodInfo SatisfyImports = typeof(IServiceProviderExtended).GetMethod(nameof(IServiceProviderExtended.SatisfyImports))!;

    protected override void EmitCreateNewService(Parameter parameter, IlContext context, LocalBuilder resultInstanceVariable)
    {
        base.EmitCreateNewService(parameter, context, resultInstanceVariable);
        if (parameter is not ImplementationTypeParameter implementationTypeParameter
            || !ShouldEmitSatisfyImports(implementationTypeParameter.Constructor.DeclaringType))
        {
            return;
        }

        ILGenerator il = context.Generator;

        il.Emit(OpCodes.Ldarg_2);
        il.Ldloc(resultInstanceVariable.LocalIndex);
        il.EmitCall(OpCodes.Callvirt, SatisfyImports, null);
    }

    private static bool ShouldEmitSatisfyImports(Type? t)
    {
        while (t is not null && t != typeof(object))
        {
            foreach (PropertyInfo property in t.GetProperties())
            {
                if (property.SetMethod is not null)
                {
                    ImportAttribute? customAttribute = property.GetCustomAttribute<ImportAttribute>();
                    if (customAttribute != null)
                    {
                        return true;
                    }
                }
            }

            t = t.BaseType;
        }

        return false;
    }
}