// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace TestProject1;

public class TestThatICameUpWithFixingOtherStuff
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
    public void test_for_internal_service_descriptions()
    {
        var sp = Setup(sc => sc.AddTransient<IExtendedSp>());
        var serviceProvider = sp.GetService<IServiceProvider>();
        var serviceProviderExtended = sp.GetService<IServiceProviderExtended>();
        var extendedSp = sp.GetService<IExtendedSp>();
        Assert.Same(serviceProvider, serviceProviderExtended);
        Assert.Same(extendedSp.Extended, sp);
        Assert.Same(extendedSp.Sp, sp);

        Assert.Same(extendedSp.Factory, sp);

        Assert.Null(extendedSp.Scope);

        using (var serviceScope = sp.CreateScope())
        {
            var sp2 = serviceScope.ServiceProvider.GetRequiredService<IExtendedSp>();
            Assert.Same(sp2.Scope, serviceScope);

            using (var scope = serviceScope.ServiceProvider.CreateScope())
            {
                var sp3 = scope.ServiceProvider.GetRequiredService<IExtendedSp>();
                Assert.Same(sp3.Scope, scope);
                Assert.NotSame(sp3, sp2);
                Assert.NotSame(sp3, sp);
            }
        }
    }

    class IExtendedSp
    {
        public IExtendedSp(IServiceProviderExtended extended,
            IServiceProvider sp,
            IServiceScopeFactory scopeFactory,
            IServiceScope scope,
            ISpCloneCreator spCloneCreator,
            IConnectionWideCache connectionWideCache)
        {
            Extended = extended;
            Sp = sp;
            Factory = scopeFactory;
            Scope = scope;
            SpCloneCreator = spCloneCreator;
            ConnectionWideCache = connectionWideCache;
        }

        public IServiceProviderExtended Extended { get; }
        public IServiceProvider Sp { get; }

        public IServiceScopeFactory Factory { get; }
        public IServiceScope Scope { get; }
        public ISpCloneCreator SpCloneCreator { get; }
        public IConnectionWideCache ConnectionWideCache { get; }
    }

    [Fact]
    public void singleton_injects_scoped_injects_service_provider()
    {
        var sp = Setup(sc => sc.AddTransient<TransientCls>().AddSingleton<SingletonCls>().AddScoped<ScopedCls>());

        IServiceProvider sp1;
        SingletonCls singletonCls1;
        IServiceProvider sp2;
        SingletonCls singletonCls2;
        using (var serviceScope = sp.CreateScope())
        {
            sp1 = serviceScope.ServiceProvider;
            singletonCls1 = serviceScope.ServiceProvider.GetRequiredService<SingletonCls>();
        }

        using (var serviceScope = sp.CreateScope())
        {
            sp2 = serviceScope.ServiceProvider;
            singletonCls2 = serviceScope.ServiceProvider.GetRequiredService<SingletonCls>();
        }

        Assert.Equal(singletonCls2.Cls.Sp, singletonCls1.Cls.Sp);
        Assert.Equal(singletonCls2.Cls.Sp, sp);
    }

    [Fact]
    public void if_singleton_throwed_once_id_does_not_deadlock()
    {
        var sp = Setup(sc => sc.AddSingleton<SingletonThatThrows>());
        Assert.Throws<Exception>(() => sp.GetService<SingletonThatThrows>());
        var a = sp.GetService<SingletonThatThrows>();
        Assert.NotNull(a);
    }

    class SingletonThatThrows
    {
        private static int count;

        public SingletonThatThrows()
        {
            if (count++ == 0)
            {
                throw new Exception();
            }
        }
    }

    class ScopedCls(IServiceProvider provider)
    {
        public IServiceProvider Sp { get; } = provider;
    }

    class SingletonCls(ScopedCls scopedCls)
    {
        public ScopedCls Cls { get; } = scopedCls;
    }

    class TransientCls
    {
    }
}