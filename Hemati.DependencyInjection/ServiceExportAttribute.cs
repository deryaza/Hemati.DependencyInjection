// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.Core;

namespace Hemati.DependencyInjection;

public class ServiceExportAttribute : Attribute
{
    public ServiceExportAttribute(Type serviceType, HbServiceLifetime lifetime)
    {
        ServiceType = serviceType;
        Lifetime = lifetime;
    }

    public Type ServiceType { get; }
    public HbServiceLifetime Lifetime { get; }
}


public class ConnectionWideImplementationOfAttribute(Type serviceType) : ServiceExportAttribute(serviceType, HbServiceLifetime.ConnectionWide);
public class ScopedImplementationOfAttribute(Type serviceType) : ServiceExportAttribute(serviceType, HbServiceLifetime.Scoped);
public class SingletonImplementationOfAttribute(Type serviceType) : ServiceExportAttribute(serviceType, HbServiceLifetime.Singleton);
public class TransientImplementationOfAttribute(Type serviceType) : ServiceExportAttribute(serviceType, HbServiceLifetime.Transient);
