// SPDX-License-Identifier: LGPL-3.0-only

using System.Diagnostics;
using Hemati.DependencyInjection.Implementation.ServiceDescriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Hemati.DependencyInjection.Implementation;

public class ServiceResolver : IServiceProviderExtended, IServiceScopeFactory, ISpCloneCreator, IDisposable
{
    public ServiceResolver(IServiceCollection serviceDescriptors, PrecomputedServiceDescriptionData[] precomputedServiceDescription, ServiceActivator activator)
    {
        if (serviceDescriptors is null)
        {
            throw new ArgumentNullException(nameof(serviceDescriptors));
        }

        if (activator is null)
        {
            throw new ArgumentNullException(nameof(activator));
        }

        Root = new(activator, ScopeRole.RootScope, this);

        activator.Descriptor.Populate(new InternalServiceDescription(InternalServiceKind.IServiceProvider));
        activator.Descriptor.Populate(new InternalServiceDescription(InternalServiceKind.IServiceProviderExtended));
        activator.Descriptor.Populate(new InternalServiceDescription(InternalServiceKind.IServiceScopeFactory));
        activator.Descriptor.Populate(new InternalServiceDescription(InternalServiceKind.IServiceScope));
        activator.Descriptor.Populate(new InternalServiceDescription(InternalServiceKind.IConnectionWideCache));
        activator.Descriptor.Populate(new InternalServiceDescription(InternalServiceKind.ISpCloneCreator));

        activator.Descriptor.Populate(serviceDescriptors);
        activator.Descriptor.Populate(precomputedServiceDescription);
    }

    private ServiceResolver(ServiceActivator activator)
    {
        Root = new(activator, ScopeRole.RootScope, this);
    }

    internal ScopeCache Root { get; }

    public ServiceActivator Activator => Root.Activator;

    public IServiceScope CreateScope()
    {
        ScopeCache scope = Root.CopyKeep(ScopeRole.ParentScope, CacheScope.Singleton);
        return scope;
    }

    public object? GetService(Type serviceType) => Root.GetService(new(serviceType), this);

    public void SatisfyImports(object? service) => Root.SatisfyImports(service, this);

    public void Populate(PrecomputedServiceDescriptionData[] serviceDescriptions) => Root.Populate(serviceDescriptions);

    public void Depopulate(string tag) => Root.Depopulate(tag);

    public void Populate(IServiceCollection serviceCollection) => Root.Populate(serviceCollection);

    public IEnumerable<IServiceDescription> GetCurrentlyRegisteredServiceDescriptions() => Root.GetCurrentlyRegisteredServiceDescriptions();

    public object? GetExportedValue(Type service, string? contract = null) => Root.GetExportedValue(service, contract);

    public void ClearAllBuildCaches() => Root.ClearAllBuildCaches();

    public void Dispose()
    {
        try
        {
            Root.Dispose();
        }
        catch (Exception)
        {
#if DEBUG
            Debugger.Break();
#endif
        }
    }

    public IServiceProvider Clone(IEnumerable<ServiceDescriptor> descriptorsToReplace)
    {
        ServiceActivator activator = Root.Activator.Clone(descriptorsToReplace);
        return new ServiceResolver(activator);
    }
}