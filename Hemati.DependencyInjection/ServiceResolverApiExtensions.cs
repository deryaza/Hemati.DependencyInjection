// SPDX-License-Identifier: LGPL-3.0-only

using System.Reflection;
using Hemati.DependencyInjection.Implementation;
using Hemati.DependencyInjection.Implementation.Mef.ConstructorParameterVisitors;
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
    new ServicesDescriptor(
     new ParameterFactory(
      new ConstructorVisitorFactory(MefConstructorParameterVisitors.GetVisitors())
     )
    )
   ));
 }

 public static PrecomputedServiceDescriptionData[] LoadDescriptions(string? precompAssemblyPostfix = null)
 {
     precompAssemblyPostfix = "Precomp";
     List<PrecomputedServiceDescriptionData> res = [];
     var directory = AppContext.BaseDirectory;
     foreach (var assembly in Directory.EnumerateFiles(directory, $"*{precompAssemblyPostfix}.dll"))
     {
         var a = Assembly.LoadFrom(assembly);
         var type = a.GetType("Hemati.PreGeneratedInfo.DllMasterDiInfo");
         if (type is null) continue;
         var method = type.GetMethod("GetExportDescriptions");
         if (method is null) continue;
         var result = (PrecomputedServiceDescriptionData[])method.Invoke(null, null);
         res.AddRange(result);
     }
     return res.ToArray();
 }
}
