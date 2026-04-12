// SPDX-License-Identifier: LGPL-3.0-only

using System.ComponentModel.Composition;
using System.Reflection;
using Hemati.DependencyInjection.Implementation.ServiceDescriptions;

namespace Hemati.DependencyInjection.Implementation;

internal static class LazyHelper
{
    private static readonly Action EmptyAction = () => { };

    public static Lazy<T> CreateFunction<T>(IServiceProvider sp, IServiceDescription description)
    {
        return new(() =>
        {
            switch (sp)
            {
                case ServiceResolver sr:
                {
                    if (sr.Activator.GetService(description, sr.Root, sr) is not T result)
                    {
                        throw new InvalidOperationException($"Failed to activate service {description.LoadServiceContract()}");
                    }

                    return result;
                }
                case ScopeCache sc:
                {
                    if (sc.Activator.GetService(description, sc, sc) is not T result)
                    {
                        throw new InvalidOperationException($"Failed to activate service {description.LoadServiceContract()}");
                    }

                    return result;
                }
            }

            throw new InvalidOperationException("Service provider was not ServiceResolver or ScopeCache");
        });
    }

    public static ExportFactory<T, TMetadata> CreateExportFactory<T, TMetadata>(
        IServiceProvider sp,
        IServiceDescription description,
        Type metadataProxyType,
        ConstructorInfo constructorInfo)
    {
        TMetadata metadata;
        Dictionary<string, object?> serviceMetadata = description.GetMetadata();
        if (metadataProxyType == typeof(Dictionary<string, object>))
        {
            metadata = (TMetadata)(object)serviceMetadata;
        }
        else
        {
            object metadataProxy = constructorInfo.Invoke([]);
            foreach ((string key, object? value) in serviceMetadata)
            {
                metadataProxyType.GetProperty(key)?.SetValue(metadataProxy, value);
            }

            metadata = (TMetadata)metadataProxy;
        }

        return new(
            () =>
            {
                switch (sp)
                {
                    case ServiceResolver sr:
                    {
                        if (sr.Activator.GetService(description, sr.Root, sr) is not T result)
                        {
                            throw new InvalidOperationException($"Failed to activate service {description.LoadServiceContract()}");
                        }

                        return (result, EmptyAction).ToTuple();
                    }
                    case ScopeCache sc:
                    {
                        if (sc.Activator.GetService(description, sc, sc) is not T result)
                        {
                            throw new InvalidOperationException($"Failed to activate service {description.LoadServiceContract()}");
                        }

                        return (result, EmptyAction).ToTuple();
                    }
                }

                throw new InvalidOperationException("Service provider was not ServiceResolver or ScopeCache");
            },
            metadata);
    }
}