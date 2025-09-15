// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Hemati.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    internal static class ServiceCollectionContainerBuilderTestExtensions
    {
        public static IServiceProvider BuildServiceProvider(this IServiceCollection services, ServiceProviderMode mode, ServiceProviderOptions options = null)
        {
            return ServiceResolverApiExtensions.BuildServiceProvider(services, []);
        }
    }
}