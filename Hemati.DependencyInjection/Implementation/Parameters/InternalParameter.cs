// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;
using Hemati.DependencyInjection.Implementation.ServiceDescriptions;

namespace Hemati.DependencyInjection.Implementation.Parameters;

public class InternalParameter : Parameter
{
    public InternalParameter(InternalServiceKind kind, Type service) : base(service, HbServiceLifetime.Transient, ImplementationInformation.Default, [])
    {
        Kind = kind;
    }

    public InternalServiceKind Kind { get; }

    public override bool Equals(Parameter? other)
    {
        return other is InternalParameter otherInternalParameter
               && Service == otherInternalParameter.Service
               && Kind == otherInternalParameter.Kind;
    }
}