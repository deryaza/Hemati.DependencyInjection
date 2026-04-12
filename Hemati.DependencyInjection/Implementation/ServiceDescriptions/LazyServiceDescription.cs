// SPDX-License-Identifier: LGPL-3.0-only

using System.Reflection;
using Hemati.DependencyInjection.Implementation.Core;

namespace Hemati.DependencyInjection.Implementation.ServiceDescriptions;

public class LazyServiceDescription(
    Type lazyType,
    Type nonLazyType,
    IServiceDescription nonLazyService,
    Type? metadataType,
    ConstructorInfo? metadataConstructorInfo) : ServiceDescriptionBase
{
    public Type NonLazyType { get; } = nonLazyType;
    public IServiceDescription NonLazyService { get; } = nonLazyService;
    public Type? MetadataType { get; } = metadataType;
    public ConstructorInfo? MetadataConstructorInfo { get; } = metadataConstructorInfo;

    public override bool IsLazy => true;

    public override BaseServiceKey GetBaseServiceKey() => new(LoadServiceContract(), null);

    public override Type LoadServiceContract() => lazyType;

    public override HbServiceLifetime GetServiceScope()
    {
        return HbServiceLifetime.Transient;
    }
}