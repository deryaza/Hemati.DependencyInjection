// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Reflection;
using Hemati.DependencyInjection.Implementation.Parameters;
using Hemati.DependencyInjection.Implementation.ServiceDescriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Hemati.DependencyInjection.Implementation;

public partial class ServicesDescriptor
{
    private readonly ConcurrentDictionary<FindServiceRequest, IServiceDescription?> _lookupCache;
    private readonly Func<FindServiceRequest, IServiceDescription?> _tryGetServiceDescriptionCoreFunc;

    public ServicesDescriptor()
    {
        _lookupCache = new(-1, 1024);
        _tryGetServiceDescriptionCoreFunc = TryGetServiceDescriptionCore;
    }

    public IServiceDescription? TryGetServiceDescription(FindServiceRequest request)
    {
        return _lookupCache.GetOrAdd(request, _tryGetServiceDescriptionCoreFunc);
    }

    protected virtual IServiceDescription? TryGetServiceDescriptionCore(FindServiceRequest request)
    {
        Type contractType = request.ServiceType;
        IServiceDescription? anyDescription = GetEveryServiceDescriptions(request).LastOrDefault();
        if (anyDescription is not null)
        {
            return anyDescription;
        }

        IServiceDescription ConstructManyForCollectionType(Type desiredServiceContract, Type? collectionType)
        {
            IServiceDescription[] descriptionsOfThatContract = GetEveryServiceDescriptions(new(desiredServiceContract)).ToArray();
            return new EnumerableDescription(contractType, desiredServiceContract, descriptionsOfThatContract, collectionType);
        }

        IServiceDescription? TryConstructEnumerable(Type openGenericServiceType, Type desiredServiceContract, Type collectionType, bool isExplicit)
        {
            if (openGenericServiceType == typeof(IEnumerable<>))
            {
                return ConstructManyForCollectionType(desiredServiceContract, null);
            }

            if (!isExplicit)
            {
                return null;
            }

            if (openGenericServiceType == typeof(List<>)
                || openGenericServiceType == typeof(IList<>))
            {
                return ConstructManyForCollectionType(
                    desiredServiceContract,
                    openGenericServiceType == typeof(List<>)
                        ? openGenericServiceType
                        : typeof(List<>).MakeGenericType(desiredServiceContract));
            }

            return null;
        }

        return request switch
        {
            { ServiceType.IsConstructedGenericType: true }
                when request.ServiceType.GetGenericTypeDefinition() is { } gdesc
                     && contractType.GetGenericArguments() is [Type singleType] => request switch
                {
                    { IsImportManyRequest: true } => TryConstructEnumerable(gdesc, singleType, request.ServiceType, true),
                    { IsImportManyRequest: false } => TryConstructEnumerable(gdesc, singleType, request.ServiceType, false),
                },
            { IsImportManyRequest: true, ServiceType.IsArray: true }
                when request.ServiceType.GetElementType() is { } el => ConstructManyForCollectionType(el, request.ServiceType),
            _ => null
        };
    }

    public void Clear() => _lookupCache.Clear();

    protected virtual ServicesDescriptor CreateCore()
    {
        return new();
    }

    public ServicesDescriptor Clone(IEnumerable<ServiceDescriptor> descriptorsToReplace)
    {
        IEnumerable<ServiceDescriptor> serviceDescriptors = descriptorsToReplace as ServiceDescriptor[] ?? descriptorsToReplace.ToArray();
        HashSet<BaseServiceKey> descriptorContractsToReplace = [.. serviceDescriptors.Select(x => new BaseServiceKey(x.ServiceType, null))];
        ServicesDescriptor res = CreateCore();

        foreach ((BaseServiceKey key, List<IServiceDescription> descriptions) in _serviceDescriptions)
        {
            if (descriptorContractsToReplace.Contains(key))
            {
                continue;
            }

            foreach (IServiceDescription serviceDescription in descriptions)
            {
                Debug.Assert(Equals(serviceDescription.GetBaseServiceKey(), key));
                res.Populate(serviceDescription);
            }
        }

        foreach (ServiceDescriptor serviceDescriptor in serviceDescriptors)
        {
            res.Populate(serviceDescriptor);
        }

        return res;
    }
}

public partial class ServicesDescriptor
{
    private readonly Dictionary<BaseServiceKey, List<IServiceDescription>> _serviceDescriptions = new();

    public IEnumerable<IServiceDescription> GetDescriptions() => _serviceDescriptions.SelectMany(x => x.Value);

    // TODO: приделать выборку по контрактам, метаданным итд
    public IEnumerable<IServiceDescription> GetEveryServiceDescriptions(FindServiceRequest request)
    {
        BaseServiceKey baseServiceKey = request.ToBaseServiceKey();
        if (_serviceDescriptions.TryGetValue(baseServiceKey, out List<IServiceDescription>? descriptionsByKey))
        {
            return descriptionsByKey;
        }

        Type requestServiceType = request.ServiceType;

        if (!requestServiceType.IsGenericType || requestServiceType.IsGenericTypeDefinition)
        {
            return [];
        }

        Type[] genericArguments = requestServiceType.GetGenericArguments();
        Type genericTypeDefinition = requestServiceType.GetGenericTypeDefinition();

        if (genericTypeDefinition == typeof(Lazy<>))
        {
            Type serviceType = genericArguments[0];
            IServiceDescription[] descriptions = GetEveryServiceDescriptions(new(serviceType)).ToArray();
            IServiceDescription[] result = new IServiceDescription[descriptions.Length];
            for (int index = 0; index < descriptions.Length; index++)
            {
                IServiceDescription serviceDescription = descriptions[index];
                Debug.Assert(index == GetImplementationNumber(serviceDescription));

                result[index] = new LazyServiceDescription(requestServiceType, serviceType, serviceDescription, null, null);
            }

            return result;
        }

        if (genericTypeDefinition == typeof(ExportFactory<,>))
        {
            Type serviceType = genericArguments[0];
            Type metadataProxyType = genericArguments[1];

            if (metadataProxyType.IsAbstract || metadataProxyType.IsInterface || metadataProxyType.GetConstructor([]) is not { } ctor)
            {
                throw new InvalidOperationException("Metadata adapter type for ExportFactory only supports constructable types (e.g. classes with public parameterless constructor)");
            }

            IServiceDescription[] descriptions = GetEveryServiceDescriptions(new(serviceType)).ToArray();
            IServiceDescription[] result = new IServiceDescription[descriptions.Length];

            for (int index = 0; index < descriptions.Length; index++)
            {
                IServiceDescription serviceDescription = descriptions[index];
                Debug.Assert(index == GetImplementationNumber(serviceDescription));

                result[index] = new LazyServiceDescription(requestServiceType, serviceType, serviceDescription, metadataProxyType, ctor);
            }

            return result;
        }

        IEnumerable<IServiceDescription>? TryGetOpenGenericImpl(Type openGenericDefinition, Type desiredServiceContract)
        {
            FindServiceRequest findServiceRequest = new(openGenericDefinition) { StringContract = request.StringContract };

            List<IServiceDescription> constrainedDescriptors = [];
            foreach (IServiceDescription openGenericDescription in GetEveryServiceDescriptions(findServiceRequest))
            {
                Type loadImplementationType = openGenericDescription.LoadImplementationType();
                if (!openGenericDescription.IsImplementationType || !loadImplementationType.IsGenericTypeDefinition)
                {
                    // хочу какой-нибудь WtfException :D
                    throw new InvalidOperationException("Open generic types can only be implemented using open Implementation Type");
                }

                Type constructedGenericType;
                try
                {
                    constructedGenericType = loadImplementationType.MakeGenericType(desiredServiceContract);
                }
                catch (ArgumentException)
                {
                    // means that constraints are not satisfied
                    continue;
                }

                ConstructedOpenGenericImplementationTypeDescription constrainedDescriptor = new(requestServiceType, constructedGenericType, openGenericDescription.GetServiceScope(), openGenericDescription);
                constrainedDescriptors.Add(constrainedDescriptor);
            }

            return constrainedDescriptors;
        }

        if (genericArguments.Length == 1 && TryGetOpenGenericImpl(genericTypeDefinition, genericArguments[0]) is { } openGenericImplementations)
        {
            return openGenericImplementations;
        }

        return Array.Empty<IServiceDescription>();
    }

    public ImplementationInformation GetImplementationInformation(IServiceDescription serviceDescription)
    {
        // Короче большой туду - переместить куда-то ImplementationInformation
        // потому что надо диспозить транзиенты если они создаются в скопе
        // это ещё про метод ниже
        return new(serviceDescription, this);
    }

    public int GetImplementationNumber(IServiceDescription serviceDescription)
    {
        if (serviceDescription is ConstructedOpenGenericImplementationTypeDescription openGeneric)
        {
            serviceDescription = openGeneric.OpenGenericServiceDescription;
        }

        if (!_serviceDescriptions.TryGetValue(serviceDescription.GetBaseServiceKey(), out List<IServiceDescription>? serviceDescriptions))
        {
            // для спэшл хендлеров вроде Lazy
            return 0;
        }

        int implementationNumber = serviceDescriptions.IndexOf(serviceDescription);
        Debug.Assert(implementationNumber >= 0);
        return implementationNumber;
    }

    internal void Populate(IServiceDescription serviceDescription)
    {
        BaseServiceKey key = serviceDescription.GetBaseServiceKey();
        if (!_serviceDescriptions.TryGetValue(key, out List<IServiceDescription>? descriptions))
        {
            _serviceDescriptions[key] = descriptions = new();
        }

        descriptions.Add(serviceDescription);
    }

    public void Populate(PrecomputedServiceDescriptionData serviceDescriptionData) => Populate(new PrecomputedServiceDescription(serviceDescriptionData));

    public void Depopulate(string tag)
    {
        IServiceDescription[] descriptorsToRemove = _serviceDescriptions.SelectMany(x => x.Value).Where(x => x.Tag == tag).ToArray();
        foreach (IServiceDescription serviceDescription in descriptorsToRemove)
        {
            _serviceDescriptions[serviceDescription.GetBaseServiceKey()].Remove(serviceDescription);
        }
    }

    public void Populate(PrecomputedServiceDescriptionData[] serviceDescriptions)
    {
        foreach (PrecomputedServiceDescriptionData precomputedServiceDescriptionData in serviceDescriptions)
        {
            Populate(precomputedServiceDescriptionData);
        }
    }

    public void Populate(ServiceDescriptor descriptor)
    {
        AspNetServiceDescriptorBasedServiceDescription descAdapter = new(descriptor);
        Populate(descAdapter);
    }

    public void Populate(IList<ServiceDescriptor> descriptors)
    {
        foreach (ServiceDescriptor serviceDescriptor in descriptors)
        {
            Populate(serviceDescriptor);
        }
    }
}