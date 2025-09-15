// SPDX-License-Identifier: LGPL-3.0-only

using System.Diagnostics;
using System.Reflection.Emit;

namespace Hemati.DependencyInjection.Implementation;

public static class BuilderExtensions
{
 [DebuggerStepThrough]
 public static void Stloc(this ILGenerator generator, int index)
 {
  switch (index)
  {
   case 0:
   {
    generator.Emit(OpCodes.Stloc_0);
    return;
   }
   case 1:
   {
    generator.Emit(OpCodes.Stloc_1);
    return;
   }
   case 2:
   {
    generator.Emit(OpCodes.Stloc_2);
    return;
   }
   case 3:
   {
    generator.Emit(OpCodes.Stloc_3);
    return;
   }
  }

  if (index < byte.MaxValue)
  {
   generator.Emit(OpCodes.Stloc_S, (byte)index);
  }
  else
  {
   generator.Emit(OpCodes.Stloc, index);
  }
 }

 [DebuggerStepThrough]
 public static void Ldloc(this ILGenerator generator, int index)
 {
  switch (index)
  {
   case 0:
   {
    generator.Emit(OpCodes.Ldloc_0);
    return;
   }
   case 1:
   {
    generator.Emit(OpCodes.Ldloc_1);
    return;
   }
   case 2:
   {
    generator.Emit(OpCodes.Ldloc_2);
    return;
   }
   case 3:
   {
    generator.Emit(OpCodes.Ldloc_3);
    return;
   }
  }

  if (index < byte.MaxValue)
  {
   generator.Emit(OpCodes.Ldloc_S, (byte)index);
  }
  else
  {
   generator.Emit(OpCodes.Ldloc, index);
  }
 }
}