// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Parameters;

namespace Hemati.DependencyInjection.Implementation;

public abstract class ServiceBuilder<TContext> : IServiceBuilder
{
    public abstract Func<ScopeCache, IServiceProviderExtended, object?> Build(Parameter parameter);

    protected abstract void VisitFactory(FactoryParameter parameter, TContext context);

    protected abstract void VisitConstant(ConstantParameter parameter, TContext context);

    protected abstract void VisitImplType(ImplementationTypeParameter parameter, TContext context);

    protected abstract void VisitUnknown(UnknownParameter parameter, TContext context);

    protected abstract void VisitEnumerable(EnumerableParameter parameter, TContext context);

    protected abstract void VisitCached(CachedObjParameter parameter, TContext context);

    protected abstract void VisitInternal(InternalParameter ip, TContext context);

    protected abstract void VisitLazy(LazyParameter lazy, TContext context);

    protected virtual void VisitMain(Parameter parameter, TContext context)
    {
        switch (parameter)
        {
            case FactoryParameter fp:
                VisitFactory(fp, context);
                break;
            case ConstantParameter cp:
                VisitConstant(cp, context);
                break;
            case ImplementationTypeParameter it:
                VisitImplType(it, context);
                break;
            case EnumerableParameter ep:
                VisitEnumerable(ep, context);
                break;
            case CachedObjParameter cp:
                VisitCached(cp, context);
                break;
            case InternalParameter ip:
                VisitInternal(ip, context);
                return;
            case LazyParameter lazyParameter:
                VisitLazy(lazyParameter, context);
                break;
            case UnknownParameter up:
                VisitUnknown(up, context);
                break;
        }
    }
}