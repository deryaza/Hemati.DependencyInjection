// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace TestProject1;

public class ScopeTests
{
    public static IServiceProvider Setup(Action<ServiceCollection>? setup = null)
    {
        ServiceCollection serviceCollection = new();
        setup?.Invoke(serviceCollection);
        return ServiceResolverApiExtensions.BuildServiceProvider(serviceCollection, []);
    }

    #region Classes

    class DisposableInfoCollector : IDisposable
    {
        public bool IsScopedDisposed;
        public bool IsSingletonDisposed;
        public readonly List<Type> DisposedTypes = new();

        public bool IsDisposed;

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    class ScopedDisposable : IDisposable
    {
        private readonly DisposableInfoCollector _disposableInfoCollector;

        public ScopedDisposable(DisposableInfoCollector disposableInfoCollector)
        {
            _disposableInfoCollector = disposableInfoCollector;
        }

        public void Dispose()
        {
            _disposableInfoCollector.IsScopedDisposed = true;
        }
    }

    class SingletonDisposable : IDisposable
    {
        private readonly DisposableInfoCollector _disposableInfoCollector;

        public SingletonDisposable(DisposableInfoCollector disposableInfoCollector)
        {
            _disposableInfoCollector = disposableInfoCollector;
        }

        public void Dispose()
        {
            _disposableInfoCollector.IsSingletonDisposed = true;
        }
    }

    class AnyDisposable : IDisposable
    {
        private readonly DisposableInfoCollector _disposableInfoCollector;

        public AnyDisposable(DisposableInfoCollector disposableInfoCollector)
        {
            _disposableInfoCollector = disposableInfoCollector;
        }

        public void Dispose()
        {
            _disposableInfoCollector.DisposedTypes.Add(GetType());
        }
    }

    class AnyService
    {
    }

    #endregion

    [Fact]
    public void cw_does_dispose_cw_on_dispose()
    {
        var provider = Setup(s =>
        {
            s.AddConnectionWide<AnyDisposable>();
            s.AddScoped<ScopedDisposable>();
            s.AddSingleton<DisposableInfoCollector>();
        });

        var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<AnyDisposable>();

        var scopedDisposable = scope.ServiceProvider.GetRequiredService<ScopedDisposable>();
        var subScope = scope.ServiceProvider.CreateScope();
        var scopedDisposable2 = subScope.ServiceProvider.GetRequiredService<ScopedDisposable>();

        subScope.ServiceProvider.GetRequiredService<AnyDisposable>();

        Assert.NotEqual(scopedDisposable, scopedDisposable2);

        var disposableInfoCollector = scope.ServiceProvider.GetRequiredService<DisposableInfoCollector>();
        var disposableInfoCollector1 = subScope.ServiceProvider.GetRequiredService<DisposableInfoCollector>();

        Assert.Equal(disposableInfoCollector, disposableInfoCollector1);

        subScope.Dispose();

        Assert.False(disposableInfoCollector.IsDisposed);
        Assert.Empty(disposableInfoCollector.DisposedTypes);
        Assert.True(disposableInfoCollector.IsScopedDisposed);

        scope.Dispose();

        Assert.False(disposableInfoCollector.IsDisposed);
        Assert.NotEmpty(disposableInfoCollector.DisposedTypes);
        Assert.True(disposableInfoCollector.DisposedTypes[0] == typeof(AnyDisposable));
        Assert.True(disposableInfoCollector.IsScopedDisposed);

        ((IDisposable)provider).Dispose();
        Assert.True(disposableInfoCollector.IsDisposed);
    }

    [Fact]
    public void cw_services_creates_only_once()
    {
        IServiceProvider provider = Setup(s => s.AddConnectionWide<AnyService>());

        IServiceScope scope = provider.CreateScope();
        AnyService s1 = scope.ServiceProvider.GetRequiredService<AnyService>();
        AnyService s2 = scope.ServiceProvider.GetRequiredService<AnyService>();
        Assert.Equal(s1, s2);

        IServiceScope scope2 = scope.ServiceProvider.CreateScope();
        AnyService s3 = scope2.ServiceProvider.GetRequiredService<AnyService>();
        Assert.Equal(s1, s3);
    }

    [Fact]
    public void can_retrieve_cached_obj_in_child_scope()
    {
        IServiceProvider provider = Setup(s => s.PromiseToAddScoped<AnyService>());

        IServiceScope scope = provider.CreateScope();
        IConnectionWideCache cache = scope.ServiceProvider.GetRequiredService<IConnectionWideCache>();
        AnyService storedObj = new AnyService();
        cache.StoreObj(storedObj);

        Ex.DoesNotThrow(() => cache.EnsureEachSatisfied());

        AnyService resolvedService = scope.ServiceProvider.GetRequiredService<AnyService>();
        Assert.Equal(storedObj, resolvedService);

        IServiceScope scope2 = scope.ServiceProvider.CreateScope();
        AnyService secondScopeService = scope2.ServiceProvider.GetRequiredService<AnyService>();
        Assert.NotNull(secondScopeService);
        Assert.Equal(storedObj, secondScopeService);
    }

    [Fact]
    public void can_retrieve_cached_obj()
    {
        IServiceProvider provider = Setup(s => s.PromiseToAddScoped<AnyService>());

        IServiceScope scope = provider.CreateScope();
        IConnectionWideCache cache = scope.ServiceProvider.GetRequiredService<IConnectionWideCache>();
        AnyService storedObj = new AnyService();
        cache.StoreObj(storedObj);

        Ex.DoesNotThrow(() => cache.EnsureEachSatisfied());

        AnyService resolvedService = scope.ServiceProvider.GetRequiredService<AnyService>();
        Assert.Equal(storedObj, resolvedService);
    }

    [Fact]
    public void Child_Scopes_does_throw_if_cached_obj_was_not_provider_on_ensure()
    {
        IServiceProvider provider = Setup(s => s.PromiseToAddScoped<AnyService>());

        IServiceScope scope = provider.CreateScope();
        IConnectionWideCache cache = scope.ServiceProvider.GetRequiredService<IConnectionWideCache>();

        Assert.Throws<InvalidOperationException>(() => cache.EnsureEachSatisfied());
    }

    [Fact]
    public void Child_Scopes_does_not_throw_if_cached_obj_was_registered()
    {
        IServiceProvider provider = Setup(s => s.PromiseToAddScoped<AnyService>());

        IServiceScope scope = provider.CreateScope();
        IConnectionWideCache cache = scope.ServiceProvider.GetRequiredService<IConnectionWideCache>();
        Ex.DoesNotThrow(() => cache.StoreObj(new AnyService()));
        Ex.DoesNotThrow(() => cache.EnsureEachSatisfied());
    }

    [Fact]
    public void Child_Scopes_does_throw_if_cached_obj_was_not_registered()
    {
        IServiceProvider provider = Setup();
        IServiceScope scope = provider.CreateScope();
        IConnectionWideCache cache = scope.ServiceProvider.GetRequiredService<IConnectionWideCache>();
        Assert.Throws<InvalidOperationException>(() => cache.StoreObj(new AnyService()));
    }

    [Fact]
    public void root_scope_does_throw_if_cached_obj_was_tried_to_be_added_to()
    {
        IServiceProvider provider = Setup(s => s.PromiseToAddScoped<AnyService>());

        IConnectionWideCache cache = provider.GetRequiredService<IConnectionWideCache>();
        Assert.Throws<InvalidOperationException>(() => cache.StoreObj(new AnyService()));
    }

    [Fact]
    public void Connection_Wide_cache_do_exist()
    {
        IServiceProvider provider = Setup();
        Assert.NotNull(provider.GetService<IConnectionWideCache>());
    }

    [Fact]
    public void Child_Scopes_does_inherit_connection_wide_services()
    {
        IServiceProvider provider = Setup(c => c.AddConnectionWide<AnyService>());

        IServiceScope parentScope = provider.CreateScope();
        AnyService anyServiceFromParent = parentScope.ServiceProvider.GetRequiredService<AnyService>();

        IServiceScope childScope = parentScope.ServiceProvider.CreateScope();
        AnyService anyServiceFromChild = childScope.ServiceProvider.GetRequiredService<AnyService>();

        Assert.True(anyServiceFromParent == anyServiceFromChild);
        Assert.Equal(anyServiceFromParent, anyServiceFromChild);
    }

    [Fact]
    public void Child_Scopes_does_not_inherit_scoped_services()
    {
        IServiceProvider provider = Setup(c => c.AddScoped<AnyService>());

        IServiceScope parentScope = provider.CreateScope();
        AnyService anyServiceFromParent = parentScope.ServiceProvider.GetRequiredService<AnyService>();

        IServiceScope childScope = parentScope.ServiceProvider.CreateScope();
        AnyService anyServiceFromChild = childScope.ServiceProvider.GetRequiredService<AnyService>();

        Assert.NotEqual(anyServiceFromParent, anyServiceFromChild);
    }

    [Fact]
    public static void TestScope_Disposing_Only_Scoped()
    {
        IServiceProvider provider = Setup(c =>
        {
            c.AddScoped<ScopedDisposable>();
            c.AddSingleton<SingletonDisposable>();
            c.AddSingleton<DisposableInfoCollector>();
        });

        IServiceScope scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ScopedDisposable>();
        scope.ServiceProvider.GetRequiredService<SingletonDisposable>();
        DisposableInfoCollector collector = scope.ServiceProvider.GetRequiredService<DisposableInfoCollector>();
        scope.Dispose();
        Assert.True(collector.IsScopedDisposed);
        Assert.False(collector.IsSingletonDisposed);
    }
}