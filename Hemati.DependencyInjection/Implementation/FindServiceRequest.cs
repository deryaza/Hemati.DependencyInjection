// SPDX-License-Identifier: LGPL-3.0-only

namespace Hemati.DependencyInjection.Implementation;

public struct FindServiceRequest(Type serviceType) : IEquatable<FindServiceRequest>
{
    public readonly Type ServiceType = serviceType;
    public bool IsImportManyRequest;
    public string? StringContract;

    public BaseServiceKey ToBaseServiceKey()
    {
        return new(ServiceType, StringContract);
    }

    public bool Equals(FindServiceRequest other)
    {
        return ServiceType == other.ServiceType && IsImportManyRequest == other.IsImportManyRequest && StringContract == other.StringContract;
    }

    public override bool Equals(object? obj)
    {
        return obj is FindServiceRequest other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ServiceType, IsImportManyRequest, StringContract);
    }
}