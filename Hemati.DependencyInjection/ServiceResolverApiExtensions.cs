// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Hemati.DependencyInjection;

public static class ServiceResolverApiExtensions
{
    public static IServiceProviderExtended BuildServiceProvider(IServiceCollection services, PrecomputedServiceDescriptionData[] precomputedServiceDescriptions)
    {
        return new ServiceResolver(
            services,
            precomputedServiceDescriptions,
            new ServiceActivator(
                new InterceptingImportAttributesBuilder(),
                new ServicesDescriptor(),
                new ParameterFactory(
                    new ConstructorVisitor()
                )
            ));
    }
}