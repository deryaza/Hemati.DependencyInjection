// SPDX-License-Identifier: LGPL-3.0-only

using System.Reflection;
using System.Reflection.Emit;
using Hemati.DependencyInjection.Implementation.Parameters;

namespace Hemati.DependencyInjection.Implementation;

public class InterceptingImportAttributesBuilder : IlServiceBuilder
{
 private static readonly MethodInfo SatisfyImports = typeof(IServiceProviderExtended).GetMethod(nameof(IServiceProviderExtended.SatisfyImports))!;

 protected override void EmitCreateNewService(Parameter parameter, IlContext context)
 {
  ILGenerator il = context.Generator;
  Label notExtendedSp = il.DefineLabel();
  Label exit = il.DefineLabel();

  LocalBuilder declareLocal = il.DeclareLocal(typeof(object));

  // puts service on the stack
  base.EmitCreateNewService(parameter, context);
  il.Stloc(declareLocal.LocalIndex);

  il.Emit(OpCodes.Ldarg_2);
  il.Emit(OpCodes.Isinst, typeof(IServiceProviderExtended));
  il.Emit(OpCodes.Dup);
  il.Emit(OpCodes.Brfalse, notExtendedSp);

  il.Ldloc(declareLocal.LocalIndex);
  il.EmitCall(OpCodes.Callvirt, SatisfyImports, null);

  il.Emit(OpCodes.Br, exit);
  il.MarkLabel(notExtendedSp);

  il.Emit(OpCodes.Pop);
  il.Ldloc(declareLocal.LocalIndex);

  il.MarkLabel(exit);
 }
}