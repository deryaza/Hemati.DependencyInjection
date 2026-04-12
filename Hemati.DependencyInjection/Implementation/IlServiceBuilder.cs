// SPDX-License-Identifier: LGPL-3.0-only

using System.Reflection;
using System.Reflection.Emit;
using Hemati.DependencyInjection.Implementation.Core;
using Hemati.DependencyInjection.Implementation.Parameters;
using Hemati.DependencyInjection.Implementation.ServiceDescriptions;

namespace Hemati.DependencyInjection.Implementation;

public class IlServiceBuilder : ServiceBuilder<IlServiceBuilder.IlContext>
{
    public class IlDelegateThisObject
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

        public bool AreWeCreatingSingleton { get; set; }

        public ILGenerator Generator { get; }
        public List<object> Constants { get; }
        public List<Func<IServiceProvider, object?>> Factories { get; }

        public void AddAndLoadConstant<T>(T constant, ILGenerator il) where T : notnull
        {
            int index = Constants.Count;
            Constants.Add(constant);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ConstantsField);
            il.Emit(OpCodes.Ldc_I4, index);
            il.Emit(OpCodes.Ldelem, typeof(T));
        }
    }

    public override Func<ScopeCache, IServiceProviderExtended, object?> Build(Parameter parameter)
    {
        DynamicMethod destinationMethod = new(
            $"FactoryOf{parameter.Service.Name}",
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard,
            typeof(object),
            [
                typeof(IlDelegateThisObject),
                typeof(ScopeCache),
                typeof(IServiceProviderExtended)
            ],
            typeof(IlContext),
            true);

        ILGenerator generator = destinationMethod.GetILGenerator();
        IlContext context = new(generator);

        VisitMain(parameter, context);
        generator.Emit(OpCodes.Ret);

        IlDelegateThisObject @this = new(context.Constants.ToArray(), context.Factories.ToArray());
        Func<ScopeCache, IServiceProviderExtended, object?> compiled = (Func<ScopeCache, IServiceProviderExtended, object?>)destinationMethod.CreateDelegate(typeof(Func<ScopeCache, IServiceProviderExtended, object?>), @this);

        /*var persistedAssemblyBuilder = new PersistedAssemblyBuilder(new AssemblyName("qweqwe"), typeof(object).Assembly);
        var defineDynamicModule = persistedAssemblyBuilder.DefineDynamicModule("qwe");
        var typeBuilder = defineDynamicModule.DefineType("Test");
        var m1 = typeBuilder.DefineMethod(
            $"FactoryOf{parameter.Service.Name}",
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard,
            typeof(object),
            [
                typeof(IlDelegateThisObject),
                typeof(ScopeCache),
                typeof(IServiceProviderExtended)
            ]
        );
        generator = m1.GetILGenerator();
        context = new(generator);
        VisitMain(parameter, context);
        generator.Emit(OpCodes.Ret);
        typeBuilder.CreateType();
        persistedAssemblyBuilder.Save("qwe.dll");*/

        return compiled;
    }

    private static readonly MethodInfo TryGetActivatedService = typeof(ScopeCache).GetMethod(nameof(ScopeCache.TryGetActivatedService))!;
    private static readonly MethodInfo StoreInCache = typeof(ScopeCache).GetMethod(nameof(ScopeCache.Store))!;
    private static readonly MethodInfo LockExit = typeof(Lock).GetMethod(nameof(Lock.Exit))!;
    private static readonly ConstructorInfo InvalidOperationExceptionConstructor = typeof(InvalidOperationException).GetConstructor([typeof(string)])!;
    private static readonly ConstructorInfo BaseServiceKeyConstructorInfo = typeof(BaseServiceKey).GetConstructor([typeof(string), typeof(string), typeof(int)])!;

    protected virtual void EmitCreateNewService(Parameter parameter, IlContext context, LocalBuilder resultInstanceVariable)
    {
        base.VisitMain(parameter, context);
        context.Generator.Stloc(resultInstanceVariable.LocalIndex);
    }

    private static readonly PropertyInfo ServiceResolverRootPropertyInfo = typeof(ServiceResolver).GetProperty(nameof(ServiceResolver.Root), BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly PropertyInfo RootResolverPropertyInfo = typeof(ScopeCache).GetProperty(nameof(ScopeCache.RootResolver), BindingFlags.Instance | BindingFlags.NonPublic)!;

    protected sealed override void VisitMain(Parameter parameter, IlContext context)
    {
        ILGenerator il = context.Generator;

        #region Locals and Labels

        LocalBuilder? varOrigCache = null;
        LocalBuilder? varOrigSp = null;
        if (parameter.Scope == HbServiceLifetime.Singleton && !context.AreWeCreatingSingleton)
        {
            context.AreWeCreatingSingleton = true;
            varOrigCache = il.DeclareLocal(typeof(ScopeCache));
            varOrigSp = il.DeclareLocal(typeof(IServiceProviderExtended));

            il.Emit(OpCodes.Ldarg_1);
            il.Stloc(varOrigCache.LocalIndex);
            il.Emit(OpCodes.Ldarg_2);
            il.Stloc(varOrigSp.LocalIndex);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, RootResolverPropertyInfo.GetMethod!);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Call, ServiceResolverRootPropertyInfo.GetMethod!);

            il.Emit(OpCodes.Starg_S, 1);
            il.Emit(OpCodes.Starg_S, 2);
        }

        LocalBuilder varBaseServiceKey = il.DeclareLocal(typeof(BaseServiceKey));
        LocalBuilder varResultInstance = il.DeclareLocal(typeof(object));

        Label exitLabel = il.DefineLabel();

        #endregion

        BaseServiceKey baseServiceKey = parameter.GetServiceKey();

        LocalBuilder? varLock = null;
        if (parameter.Scope != HbServiceLifetime.Transient)
        {
            il.Emit(OpCodes.Ldstr, baseServiceKey.TypeName);
            if (baseServiceKey.StringContract is null)
            {
                il.Emit(OpCodes.Ldnull);
            }
            else
            {
                il.Emit(OpCodes.Ldstr, baseServiceKey.StringContract);
            }

            il.Emit(OpCodes.Ldc_I4, baseServiceKey.GetHashCode());

            il.Emit(OpCodes.Newobj, BaseServiceKeyConstructorInfo);
            il.Stloc(varBaseServiceKey.LocalIndex);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4, 1 << (int)parameter.Scope);
            il.Ldloc(varBaseServiceKey.LocalIndex);
            il.Emit(OpCodes.Ldc_I4, parameter.ImplInfo.GetImplementationNumber());
            il.Ldloca(varResultInstance.LocalIndex);
            il.EmitCall(OpCodes.Call, TryGetActivatedService, null);
            il.Emit(OpCodes.Brtrue, exitLabel);

            varLock = il.DeclareLocal(typeof(Lock));
            il.Ldloc(varResultInstance.LocalIndex);
            il.Stloc(varLock.LocalIndex);
            il.BeginExceptionBlock();
        }

        EmitCreateNewService(parameter, context, varResultInstance);

        if (parameter.Scope != HbServiceLifetime.Transient)
        {
            il.Emit(OpCodes.Ldarg_1);
            il.Ldloc(varResultInstance.LocalIndex);
            il.Ldloc(varBaseServiceKey.LocalIndex);
            il.Emit(OpCodes.Ldc_I4, parameter.ImplInfo.GetImplementationNumber());
            il.Emit(OpCodes.Ldc_I4, 1 << (int)parameter.Scope);
            il.EmitCall(OpCodes.Call, StoreInCache, null);

            il.BeginFinallyBlock();
            il.Ldloc(varLock!.LocalIndex);
            il.EmitCall(OpCodes.Callvirt, LockExit, null);
            il.EndExceptionBlock();
        }

        il.MarkLabel(exitLabel);
        il.Ldloc(varResultInstance.LocalIndex);

        if (varOrigCache != null && varOrigSp != null)
        {
            context.AreWeCreatingSingleton = false;
            il.Ldloc(varOrigCache.LocalIndex);
            il.Emit(OpCodes.Starg_S, (byte)1);
            il.Ldloc(varOrigSp.LocalIndex);
            il.Emit(OpCodes.Starg_S, (byte)2);
        }
    }

    protected override void VisitInternal(InternalParameter ip, IlContext context)
    {
        var il = context.Generator;
        switch (ip.Kind)
        {
            case InternalServiceKind.IServiceProvider:
            case InternalServiceKind.IServiceProviderExtended:
            case InternalServiceKind.IServiceScopeFactory:
            {
                il.Emit(OpCodes.Ldarg_2);
                break;
            }
            case InternalServiceKind.IServiceScope:
            {
                var exit = il.DefineLabel();
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Isinst, typeof(ScopeCache));
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Brtrue, exit);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldnull);
                il.MarkLabel(exit);

                break;
            }
            case InternalServiceKind.IConnectionWideCache:
            {
                var done = il.DefineLabel();
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Isinst, typeof(ScopeCache));
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Brtrue, done);

                var ifNotServiceResolver = il.DefineLabel();
                {
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Isinst, typeof(ServiceResolver));
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Brfalse, ifNotServiceResolver);

                    il.Emit(OpCodes.Call, ServiceResolverRootPropertyInfo.GetMethod!);
                    il.Emit(OpCodes.Br, done);
                }

                il.MarkLabel(ifNotServiceResolver);
                {
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Ldnull);
                }

                il.MarkLabel(done);

                break;
            }
            case InternalServiceKind.ISpCloneCreator:
            {
                var exit = il.DefineLabel();
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Isinst, typeof(ServiceResolver));
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Brtrue, exit);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldnull);
                il.MarkLabel(exit);

                break;
            }
            default:
            {
                throw new ArgumentOutOfRangeException();
            }
        }
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
        LocalBuilder[] localBuilders = new LocalBuilder[parameter.Parameters.Length];
        for (var index = 0; index < parameter.Parameters.Length; index++)
        {
            var constructorParameter = parameter.Parameters[index];
            VisitMain(constructorParameter, context);

            var local = localBuilders[index] = il.DeclareLocal(typeof(object));
            il.Stloc(local.LocalIndex);
        }

        for (var index = 0; index < parameter.Parameters.Length; index++)
        {
            var constructorParameter = parameter.Parameters[index];
            var local = localBuilders[index];

            il.Ldloc(local.LocalIndex);
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

        Parameter[] parameters = parameter.EnumerableParameters.ToArray();

        var elementLocal = il.DeclareLocal(parameter.SingleElementType);
        var arrayLocal = il.DeclareLocal(parameter.SingleElementType.MakeArrayType());
        il.Emit(OpCodes.Ldc_I4, parameters.Length);
        il.Emit(OpCodes.Newarr, parameter.SingleElementType);
        il.Stloc(arrayLocal.LocalIndex);
        for (int i = 0; i < parameters.Length; i++)
        {
            Parameter par = parameters[i];

            VisitMain(par, context);
            if (parameter.SingleElementType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, parameter.SingleElementType);
            }

            il.Stloc(elementLocal.LocalIndex);

            il.Ldloc(arrayLocal.LocalIndex);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Ldloc(elementLocal.LocalIndex);

            il.Emit(OpCodes.Stelem, parameter.SingleElementType);
        }

        il.Ldloc(arrayLocal.LocalIndex);
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

    private static readonly MethodInfo LazyHelperCreateFunction = typeof(LazyHelper).GetMethod(nameof(LazyHelper.CreateFunction))!;
    private static readonly MethodInfo LazyHelperCreateExportFactory = typeof(LazyHelper).GetMethod(nameof(LazyHelper.CreateExportFactory))!;

    protected override void VisitLazy(LazyParameter lazy, IlContext context)
    {
        ILGenerator il = context.Generator;

        MethodInfo createLazyForType =
            lazy.IsExportFactory
                ? LazyHelperCreateExportFactory.MakeGenericMethod(lazy.NonLazyType, lazy.MetadataType)
                : LazyHelperCreateFunction.MakeGenericMethod(lazy.NonLazyType);

        il.Emit(OpCodes.Ldarg_2);
        context.AddAndLoadConstant(lazy.NonLazyDescription, il);
        if (lazy.IsExportFactory)
        {
            context.AddAndLoadConstant(lazy.MetadataType, il);
            context.AddAndLoadConstant(lazy.MetadataConstructorInfo, il);
        }

        il.EmitCall(OpCodes.Call, createLazyForType, null);
    }
}