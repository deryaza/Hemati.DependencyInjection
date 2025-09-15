// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Hemati.DependencyInjection.Implementation;

internal class DependentPropertiesSetter
{
 private static readonly MethodInfo GetServiceMethodInfo = typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService)) ?? throw new("немогунайтиметодгетсервисё-ё");
 private static readonly MethodInfo GetTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!;
 private static readonly MethodInfo GetStackCheckMethodInfo = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.EnsureSufficientExecutionStack)) ?? throw new(nameof(RuntimeHelpers.EnsureSufficientExecutionStack));

 private readonly ConcurrentDictionary<Type, Action<DependentPropertiesSetter, IServiceProvider, object, int>?> _cache = new();

 private readonly Func<Type, Action<DependentPropertiesSetter, IServiceProvider, object, int>?> _buildFunc;

 public DependentPropertiesSetter() => _buildFunc = BuildPath;

 private static void BuildSetProperty(PropertyInfo propertyInfo, Type propertyType, ILGenerator il)
 {
  il.Emit(OpCodes.Ldarg_2);
  {
   il.Emit(OpCodes.Ldarg_1);
   {
    il.Emit(OpCodes.Ldtoken, propertyType);
    il.EmitCall(OpCodes.Call, GetTypeFromHandle, null);
   }
   il.EmitCall(OpCodes.Callvirt, GetServiceMethodInfo, null);
  }
  il.EmitCall(OpCodes.Call, propertyInfo.SetMethod!, null);
 }

 private Action<DependentPropertiesSetter, IServiceProvider, object, int>? BuildPath(Type type)
 {
  List<PropertyInfo>? propertyInfos = null;
  Type? t = type;
  while (t is not null && t != typeof(object))
  {
   foreach (PropertyInfo property in t.GetProperties())
   {
    if (property.SetMethod is not null)
    {
     ImportAttribute? customAttribute = property.GetCustomAttribute<ImportAttribute>();
     if (customAttribute != null)
     {
      propertyInfos ??= [];
      propertyInfos.Add(property);
     }
    }
   }

   t = t.BaseType;
  }

  if (propertyInfos is null || propertyInfos.Count == 0)
  {
   return null;
  }

  DynamicMethod method = new(
   $"AssignForType{type.Name}",
   MethodAttributes.Public | MethodAttributes.Static,
   CallingConventions.Standard,
   null,
   [typeof(DependentPropertiesSetter), typeof(IServiceProvider), typeof(object), typeof(int)],
   typeof(DependentPropertiesSetter),
   true);

  ILGenerator il = method.GetILGenerator();

  Label returnLabel = il.DefineLabel();

  il.Emit(OpCodes.Ldarg_2);
  il.Emit(OpCodes.Brfalse, returnLabel);

  il.EmitCall(OpCodes.Call, GetStackCheckMethodInfo, null);

  il.Emit(OpCodes.Ldarg_2);
  il.Emit(OpCodes.Castclass, type);
  il.Emit(OpCodes.Starg, 2);

  foreach (PropertyInfo property in propertyInfos)
  {
   BuildSetProperty(property, property.PropertyType, il);
  }

  il.MarkLabel(returnLabel);
  il.Emit(OpCodes.Ret);
  Delegate @delegate = method.CreateDelegate(typeof(Action<DependentPropertiesSetter, IServiceProvider, object, int>));
  return (Action<DependentPropertiesSetter, IServiceProvider, object, int>?)@delegate;
 }

 private Action<DependentPropertiesSetter, IServiceProvider, object, int>? Get(Type type) => _cache.GetOrAdd(type, _buildFunc);

 public void Clear() => _cache.Clear();

 public void SetFields(IServiceProvider provider, object obj, Type type)
 {
  Action<DependentPropertiesSetter, IServiceProvider, object, int>? lambda = Get(type);
  if (lambda is null)
  {
   return;
  }

  lambda(this, provider, obj, 0);
 }
}