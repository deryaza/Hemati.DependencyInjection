// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation.ServiceDescriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Hemati.DependencyInjection;

public interface IServiceProviderExtended : IServiceProvider
{
 object? SatisfyImports(object? service);

 void Populate(PrecomputedServiceDescriptionData[] serviceDescriptions);

 void Depopulate(string tag);

 void Populate(IServiceCollection serviceCollection);

 IEnumerable<IServiceDescription> GetCurrentlyRegisteredServiceDescriptions();

 object? GetExportedValue(Type service, string? contract = null);

 void ClearAllBuildCaches();
}