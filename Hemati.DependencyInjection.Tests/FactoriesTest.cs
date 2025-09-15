// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection;
using Hemati.DependencyInjection.Implementation;
using Hemati.DependencyInjection.Implementation.Mef.ConstructorParameterVisitors;
using Hemati.DependencyInjection.Implementation.Parameters;
using Microsoft.Extensions.DependencyInjection;

namespace TestProject1;

public class FactoriesTest
{
    public static IServiceProviderExtended Setup(Action<ServiceCollection> setup = null, Action<List<PrecomputedServiceDescriptionData>> setupPrecomp = null)
    {
        ServiceCollection serviceCollection = new();
        setup?.Invoke(serviceCollection);

        List<PrecomputedServiceDescriptionData> precomputedServiceDescription = new();
        setupPrecomp?.Invoke(precomputedServiceDescription);

        return new ServiceResolver(
            serviceCollection,
            precomputedServiceDescription.ToArray(),
            new(
                new BuildeHandlesUnknownParams(),
                new(
                    new(
                        new(MefConstructorParameterVisitors.GetVisitors())))
            )
        );
    }

    [Fact]
    public void test_factory()
    {
        var resolver = Setup(
            sc => sc.AddTransient(
                fc =>
                {
                    Console.WriteLine(fc);
                    return new Testaaa();
                }));

        using (IServiceScope serviceScope = resolver.CreateScope())
        {
            var a = serviceScope.ServiceProvider.GetService<Testaaa>();
            Assert.NotNull(a);
        }
    }

    [Fact]
    public void unknown_parameter_test()
    {
        var resolver = Setup(s => s.AddTransient<Testaaa2>());
        var a = resolver.GetService<Testaaa2>();
        Assert.NotNull(a);
        Assert.NotNull(a.Aaa);
    }

    class Testaaa
    {
    }

    class Testaaa2
    {
        public Testaaa Aaa { get; }

        public Testaaa2(Testaaa aaa)
        {
            Aaa = aaa;
        }
    }

    class BuildeHandlesUnknownParams : IlServiceBuilder
    {
        protected override Func<IServiceProvider, object?>? GetUnknownParameterHandler(UnknownParameter parameter)
        {
            return sp =>
            {
                Console.WriteLine(sp);
                return new Testaaa();
            };
        }
    }
}