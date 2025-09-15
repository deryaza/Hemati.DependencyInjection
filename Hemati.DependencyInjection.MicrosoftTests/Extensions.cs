using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

public static class ExtensionsAAAA
{
    public static Type GetImplementationType(this ServiceDescriptor serviceDescriptor)
    {
        if (serviceDescriptor.ServiceKey == null)
        {
            if (serviceDescriptor.ImplementationType != null)
            {
                return serviceDescriptor.ImplementationType;
            }
            else if (serviceDescriptor.ImplementationInstance != null)
            {
                return serviceDescriptor.ImplementationInstance.GetType();
            }
            else if (serviceDescriptor.ImplementationFactory != null)
            {
                Type[]? typeArguments = serviceDescriptor.ImplementationFactory.GetType().GenericTypeArguments;

                Debug.Assert(typeArguments.Length == 2);

                return typeArguments[1];
            }
        }
        else
        {
            if (serviceDescriptor.KeyedImplementationType != null)
            {
                return serviceDescriptor.KeyedImplementationType;
            }
            else if (serviceDescriptor.KeyedImplementationInstance != null)
            {
                return serviceDescriptor.KeyedImplementationInstance.GetType();
            }
            else if (serviceDescriptor.KeyedImplementationFactory != null)
            {
                Type[]? typeArguments = serviceDescriptor.KeyedImplementationFactory.GetType().GenericTypeArguments;

                Debug.Assert(typeArguments.Length == 3);

                return typeArguments[2];
            }
        }

        Debug.Fail("ImplementationType, ImplementationInstance, ImplementationFactory or KeyedImplementationFactory must be non null");
        return null;
    }
}