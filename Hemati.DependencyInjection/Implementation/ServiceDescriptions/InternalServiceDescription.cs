// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Hemati.DependencyInjection.Implementation.ServiceDescriptions;

public sealed class InternalServiceDescription(InternalServiceKind kind) : ServiceDescriptionBase
{
    public InternalServiceKind Kind { get; } = kind;

    public override bool IsInternal => true;

    public override BaseServiceKey GetBaseServiceKey() => new(LoadServiceContract(), null);

    public override Type LoadServiceContract()
    {
        return Kind switch
        {
            InternalServiceKind.IServiceProvider => typeof(IServiceProvider),
            InternalServiceKind.IServiceProviderExtended => typeof(IServiceProviderExtended),
            InternalServiceKind.IServiceScopeFactory => typeof(IServiceScopeFactory),
            InternalServiceKind.IServiceScope => typeof(IServiceScope),
            InternalServiceKind.IConnectionWideCache => typeof(IConnectionWideCache),
            InternalServiceKind.ISpCloneCreator => typeof(ISpCloneCreator),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override HbServiceLifetime GetServiceScope()
    {
        return HbServiceLifetime.Transient;
    }
}

public enum InternalServiceKind
{
    IServiceProvider,
    IServiceProviderExtended,
    IServiceScopeFactory,
    IServiceScope,
    IConnectionWideCache,
    ISpCloneCreator
}