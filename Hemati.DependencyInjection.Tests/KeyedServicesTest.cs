// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection;
using Hemati.DependencyInjection.Implementation;
using Hemati.DependencyInjection.Implementation.Core;
using Microsoft.Extensions.DependencyInjection;

namespace TestProject1;

public interface ITestService
{
}

public class TestKeyedService : ITestService
{
}

public class TestNotKeyedService : ITestService
{
}

public class KeyedServicesTest
{
    public static IServiceProviderExtended Setup(Action<ServiceCollection> setup = null, Action<List<PrecomputedServiceDescriptionData>> setupPrecomp = null)
    {
        ServiceCollection serviceCollection = new();
        setup?.Invoke(serviceCollection);

        List<PrecomputedServiceDescriptionData> precomputedServiceDescription = new();
        setupPrecomp?.Invoke(precomputedServiceDescription);

        return ServiceResolverApiExtensions.BuildServiceProvider(serviceCollection, precomputedServiceDescription.ToArray());
    }

    [Fact]
    public void SimpleKeyedServiceTest()
    {
        IServiceProviderExtended serviceProvider = Setup(
            _ =>
            {
            },
            list =>
            {
                list.Add(
                    new PrecomputedServiceDescriptionData(
                        new BaseServiceKey(typeof(TestKeyedService), null).TypeName,
                        "key",
                        new BaseServiceKey(typeof(ITestService), null).TypeName,
                        HbServiceLifetime.Transient,
                        null,
                        null,
                        null,
                        "Tag"
                    )
                );
            });

        object exportedValue = serviceProvider.GetExportedValue(typeof(ITestService), "key");
        Assert.NotNull(exportedValue);
        Assert.IsType<TestKeyedService>(exportedValue);

        object exportedValueWithoutContract = serviceProvider.GetExportedValue(typeof(ITestService));
        Assert.Null(exportedValueWithoutContract);
    }

    [Fact]
    public void MultipleOptionsBetweenKeyedAndNotKeyed()
    {
        IServiceProviderExtended serviceProvider = Setup(
            s => s.AddTransient<ITestService, TestNotKeyedService>(),
            list =>
            {
                list.Add(
                    new PrecomputedServiceDescriptionData(
                        new BaseServiceKey(typeof(TestKeyedService), null).TypeName,
                        "key",
                        new BaseServiceKey(typeof(ITestService), null).TypeName,
                        HbServiceLifetime.Transient,
                        null,
                        null,
                        null,
                        "Tag"
                    )
                );
            });

        object exportedValue = serviceProvider.GetExportedValue(typeof(ITestService), "key");
        Assert.IsType<TestKeyedService>(exportedValue);

        object exportedValueWithoutContract = serviceProvider.GetExportedValue(typeof(ITestService));
        Assert.IsType<TestNotKeyedService>(exportedValueWithoutContract);
    }

    [Fact]
    public void SingletonKeyed()
    {
        IServiceProviderExtended serviceProvider = Setup(
            _ =>
            {
            },
            list =>
            {
                list.Add(
                    new PrecomputedServiceDescriptionData(
                        new BaseServiceKey(typeof(TestKeyedService), null).TypeName,
                        "key",
                        new BaseServiceKey(typeof(ITestService), null).TypeName,
                        HbServiceLifetime.Singleton,
                        null,
                        null,
                        null,
                        "Tag"
                    )
                );
            });

        object exportedValue = serviceProvider.GetExportedValue(typeof(ITestService), "key");
        Assert.NotNull(exportedValue);
        Assert.IsType<TestKeyedService>(exportedValue);

        object exportedValue2 = serviceProvider.GetExportedValue(typeof(ITestService), "key");
        Assert.NotNull(exportedValue2);
        Assert.IsType<TestKeyedService>(exportedValue2);

        Assert.Equal(exportedValue, exportedValue2);
    }

    [Fact]
    public void ScopedKeyed()
    {
        IServiceProviderExtended serviceProvider = Setup(
            _ =>
            {
            },
            list =>
            {
                list.Add(
                    new PrecomputedServiceDescriptionData(
                        new BaseServiceKey(typeof(TestKeyedService), null).TypeName,
                        "key",
                        new BaseServiceKey(typeof(ITestService), null).TypeName,
                        HbServiceLifetime.Scoped,
                        null,
                        null,
                        null,
                        "Tag"
                    )
                );
            });

        object exportedValueFromOtherScope;
        using (IServiceScope serviceScope = serviceProvider.CreateScope())
        {
            IServiceProviderExtended serviceScopeProvider = (IServiceProviderExtended)serviceScope.ServiceProvider;
            exportedValueFromOtherScope = serviceScopeProvider.GetExportedValue(typeof(ITestService), "key");
            Assert.NotNull(exportedValueFromOtherScope);
            Assert.IsType<TestKeyedService>(exportedValueFromOtherScope);
        }

        using (IServiceScope serviceScope = serviceProvider.CreateScope())
        {
            IServiceProviderExtended serviceScopeProvider = (IServiceProviderExtended)serviceScope.ServiceProvider;
            object exportedValue = serviceScopeProvider.GetExportedValue(typeof(ITestService), "key");
            Assert.NotNull(exportedValue);
            Assert.IsType<TestKeyedService>(exportedValue);

            object exportedValue2 = serviceScopeProvider.GetExportedValue(typeof(ITestService), "key");
            Assert.NotNull(exportedValue2);
            Assert.IsType<TestKeyedService>(exportedValue2);

            Assert.Equal(exportedValue, exportedValue2);
            Assert.NotEqual(exportedValueFromOtherScope, exportedValue);
        }
    }
}