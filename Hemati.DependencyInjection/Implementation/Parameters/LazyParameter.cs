// SPDX-License-Identifier: LGPL-3.0-only

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Hemati.DependencyInjection.Implementation.Core;
using Hemati.DependencyInjection.Implementation.ServiceDescriptions;

namespace Hemati.DependencyInjection.Implementation.Parameters;

public class LazyParameter : Parameter
{
    public LazyParameter(Type service, Type nonLazyType, IServiceDescription nonLazyDescription, Type? metadataType, ConstructorInfo? metadataConstructorInfo) : base(service, HbServiceLifetime.Transient, ImplementationInformation.Default, [])
    {
        NonLazyType = nonLazyType;
        NonLazyDescription = nonLazyDescription;
        MetadataType = metadataType;
        MetadataConstructorInfo = metadataConstructorInfo;
    }

    public Type NonLazyType { get; }
    public IServiceDescription NonLazyDescription { get; }
    public Type? MetadataType { get; }
    public ConstructorInfo? MetadataConstructorInfo { get; }

    [MemberNotNullWhen(true, nameof(MetadataConstructorInfo), nameof(MetadataType))]
    public bool IsExportFactory => MetadataType != null && MetadataConstructorInfo != null;

    public override bool Equals(Parameter? other)
    {
        return other is LazyParameter otherLazyParameter
               && Service == otherLazyParameter.Service
               && Equals(NonLazyDescription, otherLazyParameter.NonLazyDescription);
    }
}