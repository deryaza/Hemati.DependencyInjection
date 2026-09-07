// SPDX-License-Identifier: LGPL-3.0-only

using System.ComponentModel.Composition;
using Hemati.DependencyInjection;
using Hemati.DependencyInjection.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace TestProject1;

public class ReviewRegressionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void ImportManyConstructsListsWithAllRegisteredServices(int count)
    {
        var services = new ServiceCollection();
        var resources = Enumerable.Range(0, count).Select(_ => new Resource()).ToArray();
        foreach (var resource in resources) services.AddSingleton(resource);
        services.AddTransient<ListConsumer>();
        using var provider = CreateProvider(services);

        var consumer = provider.GetRequiredService<ListConsumer>();

        Assert.Equal(resources, consumer.Items);
        Assert.Equal(resources, consumer.InterfaceItems);
    }

    [Fact]
    public void WaitingCacheReadersReleaseTheActivationLock()
    {
        using var provider = CreateProvider(new ServiceCollection());
        using var scope = (ScopeCache)provider.CreateScope();
        var key = new BaseServiceKey(typeof(Resource), null);
        Assert.False(scope.TryGetActivatedService(CacheScope.Scoped, key, 0, out var pending));
        var activationLock = Assert.IsType<Lock>(pending);
        var service = new Resource();
        var results = new object?[2];
        var retainedLock = new bool[2];
        var found = new bool[2];
        var errors = new Exception?[2];
        var readers = Enumerable.Range(0, 2).Select(index => new Thread(() =>
        {
            try
            {
                found[index] = scope.TryGetActivatedService(CacheScope.Scoped, key, 0, out results[index]);
                retainedLock[index] = activationLock.IsHeldByCurrentThread;
            }
            catch (Exception exception)
            {
                errors[index] = exception;
            }
            finally
            {
                // Clean up even with the regression, so a failed test cannot strand a reader.
                if (activationLock.IsHeldByCurrentThread) activationLock.Exit();
            }
        }) { IsBackground = true }).ToArray();

        try
        {
            foreach (var reader in readers)
            {
                reader.Start();
                Assert.True(SpinWait.SpinUntil(
                    () => (reader.ThreadState & ThreadState.WaitSleepJoin) != 0,
                    TimeSpan.FromSeconds(5)));
            }
            scope.Store(service, key, 0, CacheScope.Scoped);
        }
        finally
        {
            activationLock.Exit();
        }

        foreach (var reader in readers) Assert.True(reader.Join(TimeSpan.FromSeconds(5)));
        Assert.All(errors, error => Assert.Null(error));
        Assert.All(found, value => Assert.True(value));
        Assert.All(results, result => Assert.Same(service, result));
        Assert.All(retainedLock, value => Assert.False(value));
    }

    [Fact]
    public void NestedGenericServicesHaveDistinctKeysAndResolveCorrectly()
    {
        var services = new ServiceCollection();
        services.AddSingleton<First.Nested<int>>();
        services.AddSingleton<Second.Nested<int>>();
        using var provider = CreateProvider(services);

        Assert.NotEqual(new BaseServiceKey(typeof(First.Nested<>), null),
            new BaseServiceKey(typeof(Second.Nested<>), null));
        Assert.IsType<First.Nested<int>>(provider.GetService(typeof(First.Nested<int>)));
        Assert.IsType<Second.Nested<int>>(provider.GetService(typeof(Second.Nested<int>)));
        Assert.Same(provider.GetService<First.Nested<int>>(), provider.GetService<First.Nested<int>>());
    }

    [Theory]
    [InlineData(typeof(First.Nested<int>))]
    [InlineData(typeof(GenericOuter<int>.Nested<string>))]
    [InlineData(typeof(Dictionary<string, First.Nested<int>>))]
    public void ConstructedGenericKeysCanBeLoadedByName(Type type)
    {
        Assert.Equal(type, Type.GetType(new BaseServiceKey(type, null).TypeName, throwOnError: true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProvidersDisposeOnlyOnce(bool useRoot)
    {
        var services = new ServiceCollection();
        services.AddScoped<Resource>();
        using var root = CreateProvider(services);
        using var scope = root.CreateScope();
        var provider = useRoot ? (IServiceProviderExtended)root : (IServiceProviderExtended)scope.ServiceProvider;
        var owner = (IDisposable)provider;
        var resource = provider.GetRequiredService<Resource>();

        owner.Dispose();
        owner.Dispose();

        Assert.Equal(1, resource.DisposeCount);

        if (!useRoot)
        {
            using var sibling = root.CreateScope();
            Assert.NotNull(sibling.ServiceProvider.GetService<Resource>());
        }
    }

    private static ServiceResolver CreateProvider(ServiceCollection services) =>
        (ServiceResolver)ServiceResolverApiExtensions.BuildServiceProvider(services, []);

    public class ListConsumer([ImportMany] List<Resource> items, [ImportMany] IList<Resource> interfaceItems)
    {
        public List<Resource> Items { get; } = items;
        public IList<Resource> InterfaceItems { get; } = interfaceItems;
    }

    public class Resource : IDisposable
    {
        public int DisposeCount { get; private set; }
        public void Dispose() => DisposeCount++;
    }

    public class First { public class Nested<T>; }
    public class Second { public class Nested<T>; }
    public class GenericOuter<T> { public class Nested<U>; }
}
