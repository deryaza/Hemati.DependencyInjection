// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    public class ServiceProviderEngineScopeTests
    {
        [Fact]
        public void RootEngineScopeDisposeTest()
        {
            var services = new ServiceCollection();
            ServiceProvider sp = services.BuildServiceProvider();
            var s = sp.GetRequiredService<IServiceProvider>();
            ((IDisposable)s).Dispose();

            Assert.Throws<ObjectDisposedException>(() => sp.GetRequiredService<IServiceProvider>());
        }
    }
}