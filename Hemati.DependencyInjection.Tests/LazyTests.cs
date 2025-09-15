// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace TestProject1;

public class LazyTests
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
    public void test_simple_lazy()
    {
        var sp = Setup(sc => sc.AddTransient<LazyType>().AddTransient<LazyTypeCtor>());
        Lazy<LazyType> lazyService = sp.GetService<Lazy<LazyType>>();
        Assert.NotNull(lazyService);
        var value = lazyService.Value;
        Assert.NotNull(value);
    }

    [Fact]
    public void test_ctor_lazy()
    {
        var sp = Setup(sc => sc.AddTransient<LazyType>().AddTransient<LazyTypeCtor>());
        LazyTypeCtor ctor = sp.GetService<LazyTypeCtor>();
        Assert.NotNull(ctor);
        Assert.NotNull(ctor.Lazy);
        Assert.NotNull(ctor.Lazy.Value);
    }

    [Fact]
    public void test_ctor_lazy_in_scope()
    {
        var sp = Setup(sc => sc.AddScoped<LazyType>().AddTransient<LazyTypeCtor>());
        using (IServiceScope serviceScope = sp.CreateScope())
        {
            IServiceProvider s = serviceScope.ServiceProvider;
            LazyType lazyType = s.GetService<LazyType>();
            Assert.NotNull(lazyType);
            lazyType.Scoped = "1";
            var lazyType2 = s.GetService<LazyType>();
            Assert.NotNull(lazyType2);
            Assert.Equal(lazyType2.Scoped, "1");
            Assert.Equal(lazyType, lazyType2);

            var ctor = s.GetService<LazyTypeCtor>();
            Assert.NotNull(ctor);
            Assert.NotNull(ctor.Lazy);
            Assert.NotNull(ctor.Lazy.Value);
            Assert.Equal(ctor.Lazy.Value.Scoped, "1");
            Assert.Equal(ctor.Lazy.Value, lazyType);
        }
    }

    [Fact]
    public void multiple_services_enumerable_lazy()
    {
        var sp = Setup(sc =>
        {
            sc.AddTransient<ICommonLazy, LazyType>()
                .AddTransient<LazyType>()
                .AddTransient<ICommonLazy, LazyTypeCtor>();
        });
        var services = sp.GetServices<Lazy<ICommonLazy>>();
        Assert.NotNull(services);
        var lazies = services.ToArray();
        Assert.Equal(lazies.Length, 2);
        Assert.False(lazies[0].IsValueCreated);
        Assert.False(lazies[1].IsValueCreated);

        Assert.IsAssignableFrom<LazyType>(lazies[0].Value);
        Assert.IsAssignableFrom<LazyTypeCtor>(lazies[1].Value);
        var snd = (LazyTypeCtor)lazies[1].Value;
        Assert.False(snd.Lazy.IsValueCreated);
        Assert.IsType<LazyType>(snd.Lazy.Value);
        Assert.NotSame(snd.Lazy.Value, lazies[0].Value);
    }

    [Fact]
    public void multiple_services_enumerable_lazy_but_in_scope()
    {
        var sp = Setup(sc => sc.AddTransient<ICommonLazy, LazyType>()
            .AddTransient<LazyType>()
            .AddTransient<ICommonLazy, LazyTypeCtor>());
        using (IServiceScope serviceScope = sp.CreateScope())
        {
            var sp2 = serviceScope.ServiceProvider;
            IEnumerable<Lazy<ICommonLazy>> services = sp2.GetServices<Lazy<ICommonLazy>>();
            Assert.NotNull(services);
            Lazy<ICommonLazy>[] lazies = services.ToArray();

            Assert.Equal(2, lazies.Length);
            Assert.False(lazies[0].IsValueCreated);
            Assert.False(lazies[1].IsValueCreated);

            Assert.IsType<LazyType>(lazies[0].Value);
            Assert.IsType<LazyTypeCtor>(lazies[1].Value);

            var snd = (LazyTypeCtor)lazies[1].Value;
            Assert.False(snd.Lazy.IsValueCreated);
            Assert.IsType<LazyType>(snd.Lazy.Value);

            Assert.NotSame(snd.Lazy.Value, lazies[0].Value);
        }
    }

    interface ICommonLazy
    {
    }

    class LazyType : ICommonLazy
    {
        public string Scoped;
    }

    class LazyTypeCtor : ICommonLazy
    {
        public Lazy<LazyType> Lazy { get; }

        public LazyTypeCtor(Lazy<LazyType> lazy)
        {
            Lazy = lazy;
        }
    }
}