// SPDX-License-Identifier: LGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hemati.DependencyInjection.Implementation.ServiceDescriptions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hemati.DependencyInjection.Tests
{
    public static class TestHost
    {
        // Your helper, copied verbatim (nullable annotations added)
        public static IServiceProviderExtended Setup(
            Action<ServiceCollection>? setup = null,
            Action<List<PrecomputedServiceDescriptionData>>? setupPrecomp = null)
        {
            ServiceCollection serviceCollection = new();
            setup?.Invoke(serviceCollection);

            List<PrecomputedServiceDescriptionData> precomputedServiceDescription = new();
            setupPrecomp?.Invoke(precomputedServiceDescription);

            return ServiceResolverApiExtensions.BuildServiceProvider(
                serviceCollection,
                precomputedServiceDescription.ToArray());
        }
    }

    // ---------- Test types ----------
    public interface IFoo { int Id { get; } }

    public sealed class FooA : IFoo { public int Id => 1; }
    public sealed class FooB : IFoo { public int Id => 2; }

    public sealed class NeedsFoo
    {
        public IFoo Foo { get; }
        public NeedsFoo(IFoo foo) => Foo = foo;
    }

    public readonly struct MyStruct
    {
        public int X { get; }
        public MyStruct(int x) => X = x;
    }

    public sealed class NeedsStructAndFoo
    {
        public IFoo Foo { get; }
        public MyStruct S { get; }
        public NeedsStructAndFoo(IFoo foo, MyStruct s) { Foo = foo; S = s; }
    }

    public sealed class NeedsInternalServices
    {
        public IServiceProvider Sp { get; }
        public IServiceScopeFactory ScopeFactory { get; }
        public NeedsInternalServices(IServiceProvider sp, IServiceScopeFactory scopeFactory)
        {
            Sp = sp;
            ScopeFactory = scopeFactory;
        }
    }

    public sealed class Gate
    {
        public ManualResetEventSlim Entered { get; } = new(false);
        public ManualResetEventSlim AllowFinish { get; } = new(false);
    }

    public sealed class SlowSingleton
    {
        public static int CtorCalls;
        public SlowSingleton(Gate gate)
        {
            Interlocked.Increment(ref CtorCalls);
            gate.Entered.Set();
            gate.AllowFinish.Wait();
        }
    }

    // ---------- Tests ----------
    public sealed class IlServiceBuilderIntegrationTests
    {
        [Fact]
        public void Resolving_SimpleTransient_Works_AndDoesNotThrowInvalidProgram()
        {
            var sp = TestHost.Setup(sc =>
            {
                sc.AddTransient<IFoo, FooA>();
            });

            var a = sp.GetExportedValue(typeof(IFoo));
            var b = sp.GetExportedValue(typeof(IFoo));

            Assert.NotNull(a);
            Assert.NotNull(b);
            Assert.IsType<FooA>(a);
            Assert.IsType<FooA>(b);
            Assert.NotSame(a, b);
        }

        [Fact]
        public void Resolving_ReferenceTypeCtorArg_RequiresCastclass_ButShouldWork()
        {
            // This test catches the "object passed to newobj expecting IFoo" bug.
            // If VisitImplType doesn't emit castclass for reference args, you’ll get InvalidProgramException here.
            var sp = TestHost.Setup(sc =>
            {
                sc.AddTransient<IFoo, FooA>();
                sc.AddTransient<NeedsFoo>();
            });

            var obj = sp.GetExportedValue(typeof(NeedsFoo));
            Assert.NotNull(obj);
            var nf = Assert.IsType<NeedsFoo>(obj);
            Assert.IsType<FooA>(nf.Foo);
        }

        [Fact]
        public void Resolving_ValueTypeCtorArg_UnboxAnyPath_Works()
        {
            var sp = TestHost.Setup(sc =>
            {
                sc.AddTransient<IFoo, FooA>();
                sc.AddTransient(typeof(MyStruct), _ => new MyStruct(42));
                sc.AddTransient<NeedsStructAndFoo>();
            });

            var obj = sp.GetExportedValue(typeof(NeedsStructAndFoo));
            Assert.NotNull(obj);

            var nsf = Assert.IsType<NeedsStructAndFoo>(obj);
            Assert.IsType<FooA>(nsf.Foo);
            Assert.Equal(42, nsf.S.X);
        }

        [Fact]
        public void Scoped_Lifetime_IsPerScope()
        {
            var sp = TestHost.Setup(sc =>
            {
                sc.AddScoped<IFoo, FooA>();
            });

            using var scope1 = sp.CreateScope();
            using var scope2 = sp.CreateScope();

            var a1 = (IFoo?)((IServiceProviderExtended)scope1.ServiceProvider).GetExportedValue(typeof(IFoo));
            var a2 = (IFoo?)((IServiceProviderExtended)scope1.ServiceProvider).GetExportedValue(typeof(IFoo));
            var b1 = (IFoo?)((IServiceProviderExtended)scope2.ServiceProvider).GetExportedValue(typeof(IFoo));

            Assert.NotNull(a1);
            Assert.NotNull(a2);
            Assert.NotNull(b1);

            Assert.Same(a1, a2);
            Assert.NotSame(a1, b1);
        }

        [Fact]
        public void ClearAllBuildCaches_DoesNotBreakSubsequentResolves()
        {
            var sp = TestHost.Setup(sc =>
            {
                sc.AddTransient<IFoo, FooA>();
                sc.AddTransient<NeedsFoo>();
            });

            var before = sp.GetExportedValue(typeof(NeedsFoo));
            Assert.IsType<NeedsFoo>(before);

            sp.ClearAllBuildCaches();

            var after = sp.GetExportedValue(typeof(NeedsFoo));
            Assert.IsType<NeedsFoo>(after);
        }

        [Fact]
        public async Task Singleton_ConcurrentResolve_ShouldNotReturnLockOrSecondInstance()
        {
            SlowSingleton.CtorCalls = 0;

            var gate = new Gate();
            var sp = TestHost.Setup(sc =>
            {
                sc.AddSingleton(gate);
                sc.AddSingleton<SlowSingleton>();
            });

            // Start A: triggers creation and blocks in ctor (holds the cache lock placeholder)
            var taskA = Task.Run(() => sp.GetExportedValue(typeof(SlowSingleton)));

            // Wait until ctor entered
            Assert.True(gate.Entered.Wait(TimeSpan.FromSeconds(2)));

            // Start B: should *wait* for creation to complete, not return a Lock placeholder
            var taskB = Task.Run(() => sp.GetExportedValue(typeof(SlowSingleton)));

            await Task.Delay(50);
            Assert.False(taskB.IsCompleted); // desired behavior once lock-wait is implemented

            gate.AllowFinish.Set();

            var a = await taskA;
            var b = await taskB;

            Assert.IsType<SlowSingleton>(a);
            Assert.IsType<SlowSingleton>(b);
            Assert.Same(a, b);
            Assert.Equal(1, SlowSingleton.CtorCalls);
        }
    }
}
