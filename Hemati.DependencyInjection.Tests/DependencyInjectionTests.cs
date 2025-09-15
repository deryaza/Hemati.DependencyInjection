// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation;
using Hemati.DependencyInjection.Implementation.Mef.ConstructorParameterVisitors;
using Microsoft.Extensions.DependencyInjection;

namespace TestProject1;

public class DependencyInjectionTests
{
    public interface ITest
    {
    }

    public class Test : ITest
    {
    }

    public interface ITest2
    {
    }

    public class Test2 : ITest2
    {
        public Test2(ITest2 testObj)
        {
            TestObj2 = testObj;
        }

        public Test2(ITest testObj)
        {
            TestObj = testObj;
        }

        public ITest TestObj { get; set; }
        public ITest2 TestObj2 { get; set; }
    }

    public interface IGenericTest<T>
    {
    }

    public class GenericTest<T> : IGenericTest<T>
    {
    }

    public interface IGenericWithEnumerable<T>
    {
    }

    public class GenericWithEnumerable<T> : IGenericWithEnumerable<T>
    {
        public GenericWithEnumerable(IEnumerable<ITest> test)
        {
            Test = test;
        }

        public IEnumerable<ITest> Test { get; }
    }

    public interface IIHaveInternals
    {
        IServiceProvider Provider { get; }
        IServiceScopeFactory Factory { get; }
        IServiceScope Scope { get; }
    }

    public class HaveInternals : IIHaveInternals
    {
        public HaveInternals(IServiceProvider provider, IServiceScopeFactory factory, IServiceScope scope)
        {
            Provider = provider;
            Factory = factory;
            Scope = scope;
        }

        public IServiceProvider Provider { get; }
        public IServiceScopeFactory Factory { get; }
        public IServiceScope Scope { get; }
    }

    public IServiceProvider Provider = null!;

    public DependencyInjectionTests()
    {
        ServiceCollection sc = new ServiceCollection();
        sc.AddTransient<ITest, Test>();
        sc.AddTransient<ITest2, Test2>();
        sc.AddTransient(typeof(IGenericTest<>), typeof(GenericTest<>));
        sc.AddTransient(typeof(IGenericWithEnumerable<>), typeof(GenericWithEnumerable<>));
        sc.AddSingleton<IIHaveInternals, HaveInternals>();
        Provider = new ServiceResolver(
            sc,
            [],
            new ServiceActivator(
                new InterceptingImportAttributesBuilder(),
                new ServicesDescriptor(
                    new ParameterFactory(
                        new ConstructorVisitorFactory(MefConstructorParameterVisitors.GetVisitors())
                    )
                )
            )
        );
    }

    [Fact]
    public void Usual()
    {
        object? usual = Provider.GetService(typeof(ITest));
        Assert.IsAssignableFrom<ITest>(usual);
    }

    [Fact]
    public void WithDependencyAndConstructorSelectionCheckTest()
    {
        object? dependency = Provider.GetService(typeof(ITest2));
        Assert.IsAssignableFrom<Test2>(dependency);
        Test2? d2 = dependency as Test2;
        Assert.Null(d2.TestObj2);
        Assert.NotNull(d2.TestObj);
        Assert.IsAssignableFrom<ITest>(d2.TestObj);
    }

    [Fact]
    public void SimpleGeneric()
    {
        object? generic = Provider.GetService(typeof(IGenericTest<int>));
        Assert.IsType<GenericTest<int>>(generic);
    }

    [Fact]
    public void EnumerableTest()
    {
        ITest2[] enumerable = Provider.GetServices<ITest2>().ToArray();
        Assert.Equal(enumerable.Length, 1);
        Assert.IsType<Test2>(enumerable[0]);
    }

    [Fact]
    public void GenericThatInjectsEnumerable()
    {
        object? genericThatInjectsEnumerable = Provider.GetService(typeof(IGenericWithEnumerable<int>));
        Assert.IsType<GenericWithEnumerable<int>>(genericThatInjectsEnumerable);
        GenericWithEnumerable<int> c = (GenericWithEnumerable<int>)genericThatInjectsEnumerable;
        Assert.IsAssignableFrom<IEnumerable<ITest>>(c.Test);
        ITest[] injectedEnumerable = c.Test.ToArray();
        Assert.Equal(injectedEnumerable.Length, 1);
        Assert.IsType<Test>(injectedEnumerable[0]);
    }

    [Fact]
    public void InternalsInjectionTests()
    {
        object? internalProvider = Provider.GetService(typeof(IServiceProvider));
        Assert.NotNull(internalProvider);
        object? internalFactory = Provider.GetService(typeof(IServiceScopeFactory));
        Assert.NotNull(internalFactory);
        object? internalScope = Provider.GetService(typeof(IServiceScope));
        Assert.Null(internalScope);
        object? objectThatConsumesInternal = Provider.GetService(typeof(IIHaveInternals));
        var a = Assert.IsType<HaveInternals>(objectThatConsumesInternal);
        Assert.NotNull(a.Factory);
        Assert.NotNull(a.Provider);
        Assert.Null(a.Scope);
    }
}