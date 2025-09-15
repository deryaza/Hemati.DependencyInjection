// SPDX-License-Identifier: LGPL-3.0-only

using System.ComponentModel.Composition;
using System.Reflection;
using Hemati.DependencyInjection.Implementation.ServiceDescriptions;

namespace Hemati.DependencyInjection.Implementation;

public class ConstructorVisitor
{
 private readonly ServicesDescriptor _servicesDescriptor;
 private readonly IConstructorParameterVisitor[] _parameterVisitors;

 public ConstructorVisitor(ServicesDescriptor servicesDescriptor, IConstructorParameterVisitor[] parameterVisitors)
 {
  _servicesDescriptor = servicesDescriptor;
  _parameterVisitors = parameterVisitors;
 }

 private IServiceDescription? VisitParameter(ParameterInfo info)
 {
  FindServiceRequest request = new(info.ParameterType);

  foreach (IConstructorParameterVisitor parameterVisitor in _parameterVisitors)
  {
   parameterVisitor.VisitParameter(info, ref request);
  }

  if (_servicesDescriptor.TryGetServiceDescription(request) is { } serviceDescription)
  {
   return serviceDescription;
  }

  return null;
 }

 public RankedConstructor[] CreateRanking(ConstructorInfo[] constructor)
 {
  List<RankedConstructor> res = new(constructor.Length);
  foreach (ConstructorInfo constructorInfo in constructor)
  {
   bool isImportingConstructor = constructorInfo.GetCustomAttribute<ImportingConstructorAttribute>() is not null;

   int missingCount = 0;
   ParameterInfo[] parameterInfos = constructorInfo.GetParameters();
   IServiceDescription?[] descriptions = new IServiceDescription?[parameterInfos.Length];
   for (int index = 0; index < parameterInfos.Length; index++)
   {
    ParameterInfo parameterInfo = parameterInfos[index];
    IServiceDescription? serviceDescription = VisitParameter(parameterInfo);
    if (serviceDescription is null)
    {
     missingCount++;
    }

    descriptions[index] = serviceDescription;
   }

   res.Add(new(constructorInfo, descriptions, parameterInfos, isImportingConstructor, missingCount));
  }

  return res
   .OrderByDescending(x => x.IsImportingConstructor)
   .ThenBy(x => x.MissingParametersCount)
   .ToArray();
 }
}

public record struct RankedConstructor(
 ConstructorInfo Constructor,
 IServiceDescription?[] ServiceDescriptions,
 ParameterInfo[] Parameters,
 bool IsImportingConstructor,
 int MissingParametersCount
);