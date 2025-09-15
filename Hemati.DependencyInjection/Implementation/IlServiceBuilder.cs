// SPDX-License-Identifier: LGPL-3.0-only

using System.Reflection;
using System.Reflection.Emit;
using Hemati.DependencyInjection.Implementation.Parameters;

namespace Hemati.DependencyInjection.Implementation;

public partial class IlServiceBuilder : ServiceBuilder<IlServiceBuilder.IlContext>
{
 internal class IlDelegateThisObject
 {
  public readonly object[] Constants;
  public readonly Func<IServiceProvider, object?>[] Factories;

  public IlDelegateThisObject(object[] constants, Func<IServiceProvider, object?>[] factories)
  {
   Constants = constants;
   Factories = factories;
  }
 }

 public class IlContext
 {
  public IlContext(ILGenerator generator)
  {
   Constants = [];
   Factories = [];
   Generator = generator;
  }

  public ILGenerator Generator { get; }
  public List<object> Constants { get; }
  public List<Func<IServiceProvider, object?>> Factories { get; }
 }

 public override Func<ScopeCache, IServiceProvider, object?> Build(Parameter parameter)
 {
  DynamicMethod destinationMethod = new(
   $"FactoryOf{parameter.Service.Name}",
   MethodAttributes.Public | MethodAttributes.Static,
   CallingConventions.Standard,
   typeof(object),
   [
    typeof(IlDelegateThisObject),
    typeof(ScopeCache),
    typeof(IServiceProvider)
   ],
   typeof(IlContext),
   true);

  ILGenerator generator = destinationMethod.GetILGenerator();
  IlContext context = new(generator);

  VisitMain(parameter, context);
  generator.Emit(OpCodes.Ret);

  IlDelegateThisObject @this = new(context.Constants.ToArray(), context.Factories.ToArray());
  Func<ScopeCache, IServiceProvider, object?> compiled = (Func<ScopeCache, IServiceProvider, object?>)destinationMethod.CreateDelegate(typeof(Func<ScopeCache, IServiceProvider, object?>), @this);

  return compiled;
 }

 private static readonly MethodInfo IsAlreadyActivated = typeof(ScopeCache).GetMethod(nameof(ScopeCache.IsAlreadyActivated))!;
 private static readonly MethodInfo GetActivatedService = typeof(ScopeCache).GetMethod(nameof(ScopeCache.GetActivatedService))!;
 private static readonly MethodInfo StoreInCache = typeof(ScopeCache).GetMethod(nameof(ScopeCache.Store))!;
 private static readonly ConstructorInfo InvalidOperationExceptionConstructor = typeof(InvalidOperationException).GetConstructor([typeof(string)])!;
 private static readonly ConstructorInfo BaseServiceKeyConstructorInfo = typeof(BaseServiceKey).GetConstructor([typeof(string), typeof(string)])!;

 protected virtual void EmitCreateNewService(Parameter parameter, IlContext context)
 {
  base.VisitMain(parameter, context);
 }

 protected sealed override void VisitMain(Parameter parameter, IlContext context)
 {
  ILGenerator il = context.Generator;

  #region Locals and Labels

  LocalBuilder serviceTypeVariable = il.DeclareLocal(typeof(BaseServiceKey));
  Label getFromCacheLabel = il.DefineLabel();
  Label exitLabel = il.DefineLabel();

  #endregion

  #region if (serviceActivated)

  BaseServiceKey baseServiceKey = parameter.GetServiceKey();

  il.Emit(OpCodes.Ldstr, baseServiceKey.TypeName);

  if (baseServiceKey.StringContract is null)
  {
   il.Emit(OpCodes.Ldnull);
  }
  else
  {
   il.Emit(OpCodes.Ldstr, baseServiceKey.StringContract);
  }

  il.Emit(OpCodes.Newobj, BaseServiceKeyConstructorInfo);
  il.Stloc(serviceTypeVariable.LocalIndex);

  il.Emit(OpCodes.Ldarg_1); // ScopeCache

  il.Emit(OpCodes.Ldarg_1);
  il.Ldloc(serviceTypeVariable.LocalIndex);
  il.Emit(OpCodes.Ldc_I4, parameter.ImplInfo.GetImplementationNumber());
  il.EmitCall(OpCodes.Call, IsAlreadyActivated, null);
  il.Emit(OpCodes.Dup);

  il.Emit(OpCodes.Brtrue, getFromCacheLabel);
  il.Emit(OpCodes.Pop);

  #endregion

  EmitCreateNewService(parameter, context);
  il.Ldloc(serviceTypeVariable.LocalIndex);
  il.Emit(OpCodes.Ldc_I4, parameter.ImplInfo.GetImplementationNumber());
  il.Emit(OpCodes.Ldc_I4, 1 << (int)parameter.Scope);
  il.EmitCall(OpCodes.Call, StoreInCache, null);

  il.Emit(OpCodes.Br, exitLabel);

  #region Get from cache

  il.MarkLabel(getFromCacheLabel);
  il.Ldloc(serviceTypeVariable.LocalIndex);
  il.Emit(OpCodes.Ldc_I4, parameter.ImplInfo.GetImplementationNumber());
  il.EmitCall(OpCodes.Call, GetActivatedService, null);

  #endregion

  il.MarkLabel(exitLabel);
 }

 protected override void VisitCached(CachedObjParameter parameter, IlContext context)
 {
  ILGenerator il = context.Generator;
  il.Emit(OpCodes.Ldstr, $"Requested instance {parameter.Service.Name} was not cached");
  il.Emit(OpCodes.Newobj, InvalidOperationExceptionConstructor);
  il.Emit(OpCodes.Throw);
 }

 private static readonly FieldInfo ConstantsField = typeof(IlDelegateThisObject).GetField(nameof(IlDelegateThisObject.Constants))!;

 protected sealed override void VisitConstant(ConstantParameter parameter, IlContext context)
 {
  ILGenerator il = context.Generator;
  il.Emit(OpCodes.Ldarg_0);
  il.Emit(OpCodes.Ldfld, ConstantsField);

  int index = context.Constants.Count;
  context.Constants.Add(parameter.Impl);

  il.Emit(OpCodes.Ldc_I4, index);
  il.Emit(OpCodes.Ldelem, typeof(object));
 }

 public static readonly FieldInfo FactoriesField = typeof(IlDelegateThisObject).GetField(nameof(IlDelegateThisObject.Factories))!;

 public static readonly MethodInfo FuncInvokeMethod = typeof(Func<IServiceProvider, object?>).GetMethod(nameof(Func<IServiceProvider, object?>.Invoke))!;

 protected sealed override void VisitFactory(FactoryParameter parameter, IlContext context)
 {
  ILGenerator il = context.Generator;
  int index = context.Factories.Count;
  context.Factories.Add(parameter.Factory);

  il.Emit(OpCodes.Ldarg_0);
  il.Emit(OpCodes.Ldfld, FactoriesField);
  il.Emit(OpCodes.Ldc_I4, index);
  il.Emit(OpCodes.Ldelem, typeof(Func<IServiceProvider, object?>));
  il.Emit(OpCodes.Ldarg_2);
  il.Emit(OpCodes.Call, FuncInvokeMethod);
 }

 // index 0 - ILDelegateThisObject
 // index 1 - ScopeCache(ServiceActivator)

 protected sealed override void VisitImplType(ImplementationTypeParameter parameter, IlContext context)
 {
  ILGenerator il = context.Generator;
  foreach (Parameter constructorParameter in parameter.Parameters)
  {
   VisitMain(constructorParameter, context);
   if (constructorParameter.Service.IsValueType)
   {
    il.Emit(OpCodes.Unbox_Any, constructorParameter.Service);
   }
  }

  il.Emit(OpCodes.Newobj, parameter.Constructor);
  if (parameter.Constructor.DeclaringType!.IsValueType)
  {
   il.Emit(OpCodes.Box, parameter.Constructor.DeclaringType);
  }
 }

 protected sealed override void VisitUnknown(UnknownParameter parameter, IlContext context)
 {
  if (GetUnknownParameterHandler(parameter) is { } visitor)
  {
   ILGenerator il = context.Generator;
   int index = context.Factories.Count;
   context.Factories.Add(visitor);

   il.Emit(OpCodes.Ldarg_0);
   il.Emit(OpCodes.Ldfld, FactoriesField);
   il.Emit(OpCodes.Ldc_I4, index);
   il.Emit(OpCodes.Ldelem, typeof(Func<IServiceProvider, object?>));
   il.Emit(OpCodes.Ldarg_1);
   il.Emit(OpCodes.Call, FuncInvokeMethod);
  }
  else
  {
   context.Generator.Emit(OpCodes.Ldnull);
  }
 }

 protected sealed override void VisitEnumerable(EnumerableParameter parameter, IlContext context)
 {
  ILGenerator il = context.Generator;

  List<Parameter> parameters = parameter.EnumerableParameters.ToList();

  il.Emit(OpCodes.Ldc_I4, parameters.Count);
  il.Emit(OpCodes.Newarr, parameter.SingleElementType);
  for (int i = 0; i < parameters.Count; i++)
  {
   Parameter par = parameters[i];
   il.Emit(OpCodes.Dup);

   il.Emit(OpCodes.Ldc_I4, i);
   VisitMain(par, context);
   if (parameter.SingleElementType.IsValueType)
   {
    il.Emit(OpCodes.Unbox_Any, parameter.SingleElementType);
   }

   il.Emit(OpCodes.Stelem, parameter.SingleElementType);
  }

  if (parameter.RequestedCollectionType is not { } colType)
  {
   return;
  }

  if (colType.IsArray)
  {
   return;
  }

  ConstructorInfo? ctor = colType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(parameter.SingleElementType)]);
  if (ctor is null)
  {
   throw new InvalidOperationException("Requested collection type had no accessible constructors for creation (with single IEnumerable<elementType>)");
  }

  il.Emit(OpCodes.Newobj, ctor);
 }

 protected virtual Func<IServiceProvider, object?>? GetUnknownParameterHandler(UnknownParameter parameter) => null;
}