// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Concurrent;
using Hemati.DependencyInjection.Implementation.Core;
using Hemati.DependencyInjection.Implementation.Parameters;
using Hemati.DependencyInjection.Implementation.ServiceDescriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Hemati.DependencyInjection.Implementation;

public class ServiceActivator
{
    private readonly Func<IServiceDescription, Func<ScopeCache, IServiceProviderExtended, object?>> _buildDescriptionFunc;
    private readonly ConcurrentDictionary<IServiceDescription, Func<ScopeCache, IServiceProviderExtended, object?>> _builtDescriptions;

    private readonly Func<FindServiceRequest, Func<ScopeCache, IServiceProviderExtended, object?>> _buildServiceRequestFunc;
    private readonly ConcurrentDictionary<FindServiceRequest, Func<ScopeCache, IServiceProviderExtended, object?>> _requestCache;

    private readonly DependentPropertiesSetter _dependentPropertiesSetter = new();

    public readonly IServiceBuilder Builder;
    public readonly ServicesDescriptor Descriptor;
    private readonly ParameterFactory _parameterFactory;

    public ServiceActivator(IServiceBuilder builder, ServicesDescriptor descriptor, ParameterFactory parameterFactory)
    {
        _builtDescriptions = new(-1, 1024);
        _requestCache = new(-1, 1024);
        _buildDescriptionFunc = BuildDescription; // cached, so no allocations on each GetService call
        _buildServiceRequestFunc = BuildServiceRequest;

        Builder = builder;
        Descriptor = descriptor;
        _parameterFactory = parameterFactory;
    }

    protected virtual Func<ScopeCache, IServiceProviderExtended, object?> BuildCore(Parameter parameter) => Builder.Build(parameter);

    private Func<ScopeCache, IServiceProviderExtended, object?> BuildDescription(IServiceDescription serviceDescription) =>
        BuildCore(_parameterFactory.CreateFromServiceDescription(serviceDescription, Descriptor, new(null, serviceDescription)));

    private Func<ScopeCache, IServiceProviderExtended, object?> BuildServiceRequest(FindServiceRequest findServiceRequest)
    {
        var desc = Descriptor.TryGetServiceDescription(findServiceRequest);
        if (desc != null)
        {
            return _builtDescriptions.GetOrAdd(desc, _buildDescriptionFunc);
        }

        return BuildCore(new UnknownParameter(findServiceRequest.ServiceType, HbServiceLifetime.Transient));
    }

    public virtual object? GetService(FindServiceRequest findServiceRequest, ScopeCache cache, IServiceProviderExtended caller)
    {
        return _requestCache.GetOrAdd(findServiceRequest, _buildServiceRequestFunc)(cache, caller);
    }

    internal object? GetService(IServiceDescription serviceDescription, ScopeCache cache, IServiceProviderExtended caller)
    {
        return _builtDescriptions.GetOrAdd(serviceDescription, _buildDescriptionFunc)(cache, caller);
    }

    internal void SatisfyImports(object? service, IServiceProvider serviceProvider)
    {
        if (service == null)
        {
            return;
        }

        _dependentPropertiesSetter.SetFields(serviceProvider, service, service.GetType());
    }

    public void ClearCaches()
    {
        _builtDescriptions.Clear();
        _requestCache.Clear();
        _dependentPropertiesSetter.Clear();
        Descriptor.Clear();
    }

    public ServiceActivator Clone(IEnumerable<ServiceDescriptor> descriptorsToReplace)
    {
        return new(Builder, Descriptor.Clone(descriptorsToReplace), _parameterFactory);
    }
}