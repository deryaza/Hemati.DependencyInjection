// SPDX-License-Identifier: LGPL-3.0-only

using System.Reflection;
using Hemati.DependencyInjection.Implementation.Core;
using Hemati.DependencyInjection.Implementation.Parameters;
using Hemati.DependencyInjection.Implementation.ServiceDescriptions;

namespace Hemati.DependencyInjection.Implementation;

public class ParameterFactory
{
 public ConstructorVisitorFactory VisitorsFactory { get; }

 public ParameterFactory(ConstructorVisitorFactory visitorFactory)
 {
  VisitorsFactory = visitorFactory;
 }

 protected virtual Parameter GetCachedParameter(ServicesDescriptor serviceDescriptor, IServiceDescription serviceDescription)
 {
  return new CachedObjParameter(serviceDescription.LoadServiceContract(), serviceDescriptor.GetImplementationInformation(serviceDescription));
 }

 protected virtual Parameter GetFactoryParameter(
  ServicesDescriptor servicesDescriptor,
  IServiceDescription service,
  Func<IServiceProvider, object?> factory)
 {
  return new FactoryParameter(
   factory,
   servicesDescriptor.GetImplementationInformation(service),
   service.LoadServiceContract(),
   service.GetServiceScope()
  );
 }

 protected virtual Parameter GetConstantParameter(ServicesDescriptor servicesDescriptor, IServiceDescription service, object impl)
 {
  return new ConstantParameter(
   impl,
   service.LoadServiceContract(),
   servicesDescriptor.GetImplementationInformation(service),
   service.GetServiceScope()
  );
 }

 protected virtual Parameter GetUnknownParameter(Type service, HbServiceLifetime scope)
 {
  return new UnknownParameter(service, scope);
 }

 private bool TryFillParameters(
  IServiceDescription parentDescription,
  ServicesDescriptor servicesDescriptor,
  CyclesSupervisor supervisor,
  RankedConstructor rankedCtor,
  Parameter[] parameters,
  bool tolerateNullDescriptionsAsUnknown)
 {
  if (supervisor.HasCycle())
  {
   return false;
  }

  for (var i = 0; i < rankedCtor.ServiceDescriptions.Length; i++)
  {
   IServiceDescription? serviceDescription = rankedCtor.ServiceDescriptions[i];
   if (serviceDescription is null)
   {
    if (tolerateNullDescriptionsAsUnknown)
    {
     parameters[i] = GetUnknownParameter(rankedCtor.Parameters[i].ParameterType, parentDescription.GetServiceScope());
     continue;
    }
    else
    {
     return false;
    }
   }

   supervisor.Remember(serviceDescription);
   if (supervisor.HasCycle())
   {
    return false;
   }

   if (servicesDescriptor.CreateParameter(serviceDescription, supervisor) is not { } parameter)
   {
    return false;
   }

   parameters[i] = parameter;
  }

  return true;
 }

 protected virtual Parameter GetByImplementationParameter(ServicesDescriptor servicesDescriptor, CyclesSupervisor supervisor, IServiceDescription serviceDescription)
 {
  ConstructorVisitor visitor = VisitorsFactory.Produce(servicesDescriptor);

  ConstructorInfo[] constructorCandidates = serviceDescription.LoadImplementationType().GetConstructors();
  RankedConstructor[] ranking = visitor.CreateRanking(constructorCandidates);

  foreach (RankedConstructor rankedCtor in ranking)
  {
   CyclesSupervisor supervisorSnapshot = supervisor.Clone();
   Parameter[] parameters = new Parameter[rankedCtor.ServiceDescriptions.Length];
   if (TryFillParameters(serviceDescription, servicesDescriptor, supervisorSnapshot, rankedCtor, parameters, tolerateNullDescriptionsAsUnknown: false))
   {
    supervisor.CloneFrom(supervisorSnapshot);
    return new ImplementationTypeParameter(serviceDescription.LoadServiceContract(), serviceDescription.GetServiceScope(), rankedCtor.Constructor, servicesDescriptor.GetImplementationInformation(serviceDescription), parameters);
   }
  }

  if (ranking.Length < 1)
  {
   throw new ArgumentOutOfRangeException(nameof(serviceDescription), $"Can't find accessible constructor for type {serviceDescription.LoadImplementationType()}");
  }

  // if TryFillParameters couldn't completely fill
  // any or zero constructors but there is still any
  {
   RankedConstructor rankedCtor = ranking[0];
   CyclesSupervisor supervisorSnapshot = supervisor.Clone();
   Parameter[] parameters = new Parameter[rankedCtor.ServiceDescriptions.Length];
   if (!TryFillParameters(serviceDescription, servicesDescriptor, supervisorSnapshot, rankedCtor, parameters, tolerateNullDescriptionsAsUnknown: true))
   {
    throw new ArgumentOutOfRangeException(nameof(serviceDescription), $"Can't find accessible constructor for type {serviceDescription.LoadImplementationType()}");
   }

   supervisor.CloneFrom(supervisorSnapshot);
   return new ImplementationTypeParameter(serviceDescription.LoadServiceContract(), serviceDescription.GetServiceScope(), rankedCtor.Constructor, servicesDescriptor.GetImplementationInformation(serviceDescription), parameters);
  }
 }

 protected virtual Parameter GetEnumerableParameter(
  ServicesDescriptor serviceDescriptor,
  CyclesSupervisor supervisor,
  IServiceDescription serviceDescription)
 {
  (IEnumerable<IServiceDescription> elementDescriptions, Type elementContractType, Type? requestedCollectionType) = serviceDescription.GetEnumerableDescription();
  List<Parameter> implementations = new();

  CyclesSupervisor supervisorSnapshot = supervisor.Clone();
  foreach (IServiceDescription elementDescription in elementDescriptions)
  {
   supervisorSnapshot.Remember(elementDescription);
   CyclesSupervisor elementSupervisor = supervisorSnapshot.Dive(elementDescription);
   Parameter oneElement = CreateFromServiceDescription(elementDescription, serviceDescriptor, elementSupervisor);
   implementations.Add(oneElement);
  }

  if (supervisorSnapshot.HasCycle())
  {
   throw new InvalidOperationException("Requested enumerable had cycles in it.");
  }

  return new EnumerableParameter(
   serviceDescription.LoadServiceContract(),
   requestedCollectionType,
   elementContractType,
   HbServiceLifetime.Transient,
   implementations);
 }

 public Parameter CreateFromServiceDescription(IServiceDescription serviceDescription, ServicesDescriptor descriptor, CyclesSupervisor supervisor)
 {
  Parameter parameter;
  if (serviceDescription.IsPromiseToAddServiceDescriptor)
  {
   parameter = GetCachedParameter(descriptor, serviceDescription);
  }
  else if (serviceDescription.IsImplementationFactory)
  {
   parameter = GetFactoryParameter(descriptor, serviceDescription, serviceDescription.LoadFactory());
  }
  else if (serviceDescription.IsImplementationInstance)
  {
   parameter = GetConstantParameter(descriptor, serviceDescription, serviceDescription.LoadImplementationInstance());
  }
  else if (serviceDescription.IsImplementationType)
  {
   parameter = GetByImplementationParameter(descriptor, supervisor, serviceDescription);
  }
  else if (serviceDescription.IsEnumerableType)
  {
   parameter = GetEnumerableParameter(descriptor, supervisor, serviceDescription);
  }
  else
  {
   throw new InvalidOperationException($"Can't find a way to implement {serviceDescription}.");
  }

  return parameter;
 }
}