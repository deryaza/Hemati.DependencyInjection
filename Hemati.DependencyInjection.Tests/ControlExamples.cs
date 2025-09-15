// SPDX-License-Identifier: LGPL-3.0-only

using System.ComponentModel.Composition;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;

using Hemati.DependencyInjection;
using Hemati.DependencyInjection.Implementation;
using Hemati.DependencyInjection.Implementation.Core;
using Hemati.DependencyInjection.Implementation.ServiceDescriptions;
using Microsoft.Extensions.DependencyInjection;

using PluginLib;

namespace TestProject1;

public class ControlExamples
{
 public ControlExamples()
 {
  
 }
 [Fact]
 public void _1()
 {
  object MySingletonService = new();
  object MyScopedService = new();
  ScopeCache root = new(new(new IlServiceBuilder(), new(new(new([])))), ScopeRole.RootScope);

  root.Store(MySingletonService, new(typeof(object), null), 0, CacheScope.Singleton);
  Assert.Equal(CacheScope.Singleton, root.IsAlreadyActivated(new(typeof(object), null), 0));
  Assert.Same(root.GetActivatedService(CacheScope.Singleton, new(typeof(object), null), 0), MySingletonService);

  root.Store(MyScopedService, new(typeof(object), null), 1, CacheScope.Scoped);
  Assert.Equal(CacheScope.Scoped, root.IsAlreadyActivated(new(typeof(object), null), 1));
  Assert.Same(root.GetActivatedService(CacheScope.Scoped, new(typeof(object), null), 1), MyScopedService);

  ScopeCache scopeCache = root.CopyKeep(ScopeRole.ParentScope, CacheScope.Singleton);
  Assert.Equal(CacheScope.Singleton, scopeCache.IsAlreadyActivated(new(typeof(object), null), 0));
  Assert.Same(scopeCache.GetActivatedService(CacheScope.Singleton, new(typeof(object), null), 0), MySingletonService);

  Assert.Equal(CacheScope.None, scopeCache.IsAlreadyActivated(new(typeof(object), null), 1));
 }

 [Fact]
 public void _2()
 {
  IServiceDescription root = new DummyServiceDescription();
  CyclesSupervisor cyclesSupervisor = new(null, root);
  Assert.False(cyclesSupervisor.HasCycle());
  cyclesSupervisor.Remember(root);
  Assert.True(cyclesSupervisor.HasCycle());
  cyclesSupervisor.Forget(root);
  IServiceDescription child = new DummyServiceDescription();
  cyclesSupervisor.Remember(child);
  CyclesSupervisor cs2 = cyclesSupervisor.Dive(child);
  Assert.False(cyclesSupervisor.HasCycle());
  cs2.Remember(root);
  Assert.True(cyclesSupervisor.HasCycle());
 }

 [Fact]
 public void _3()
 {
  ServicesDescriptor descriptor = new(new(null));
  ServiceCollection serviceCollection = new();
  serviceCollection.AddTransient<IService, Service>();
  serviceCollection.AddScoped<IService2>(_ => new Service2());
  serviceCollection.AddSingleton<IService3>(new Service3());
  descriptor.Populate(serviceCollection);
  IServiceDescription[] d = descriptor.GetDescriptions().ToArray();
  Assert.Equal(3, d.Length);
  Assert.True(d[0].IsImplementationType);
  Assert.Equal(typeof(IService), d[0].LoadServiceContract());
  Assert.Equal(typeof(Service), d[0].LoadImplementationType());
  Assert.True(d[1].IsImplementationFactory);
  Assert.Equal(typeof(IService2), d[1].LoadServiceContract());
  Assert.IsType<Service2>(d[1].LoadFactory()(null!));
  Assert.True(d[2].IsImplementationInstance);
  Assert.Equal(typeof(IService3), d[2].LoadServiceContract());
  Assert.IsType<Service3>(d[2].LoadImplementationInstance());
 }

 [Fact]
 public void _4()
 {
  ServiceCollection sc = new();
  sc.AddTransient<IService, Service>();
  var sp = ServiceResolverApiExtensions.BuildServiceProvider(sc, [
   new PrecomputedServiceDescriptionData(
    implementationType: new BaseServiceKey(typeof(Service2), null).TypeName,
    keyLikeContract: "key",
    contractType: new BaseServiceKey(typeof(IService2), null).TypeName,
    creationPolicy: HbServiceLifetime.Transient,
    customAttributeType: null,
    customAttributeCtorArgsCreator: null,
    metadata: null,
    tag: "Tag"
   )
   ]);
  Assert.IsType<Service>(sp.GetService(typeof(IService)));
  Assert.Null(sp.GetExportedValue(typeof(IService2), contract: null));
  Assert.IsType<Service2>(sp.GetExportedValue(typeof(IService2), "key"));
 }

 [Fact]
 public void _5()
 {
  ServiceCollection sc = new();
  sc.AddTransient(typeof(IRepository<>), typeof(Repository<>));
  var sp = ServiceResolverApiExtensions.BuildServiceProvider(sc, []);
  var userRepository = sp.GetService(typeof(IRepository<User>));
  Assert.IsType<Repository<User>>(userRepository);
 }

 [Fact]
 public void _6()
 {
  ServiceCollection sc = new();
  sc.AddTransient<ILogger, ConsoleLogger>();
  sc.AddTransient<ILogger, FileLogger>();
  var sp = ServiceResolverApiExtensions.BuildServiceProvider(sc, []);
  var loggers = sp.GetServices<ILogger>().ToArray();
  Assert.Equal(2, loggers.Length);
  Assert.IsType<ConsoleLogger>(loggers[0]);
  Assert.IsType<FileLogger>(loggers[1]);
 }

 private static PrecomputedServiceDescriptionData CreateMefDesc<T, TImpl>(string? key = null, Dictionary<string, object?>? metadata = null)
 {
  return new PrecomputedServiceDescriptionData(
    implementationType: new BaseServiceKey(typeof(TImpl), null).TypeName,
    keyLikeContract: key,
    contractType: new BaseServiceKey(typeof(T), null).TypeName,
    creationPolicy: HbServiceLifetime.Transient,
    customAttributeType: null,
    customAttributeCtorArgsCreator: null,
    metadata: metadata,
    tag: "Tag"
   );
 }

 [Fact]
 public void _7()
 {
  var sp = ServiceResolverApiExtensions.BuildServiceProvider(new ServiceCollection(), [
   CreateMefDesc<IPlugin, PluginA>("PluginA", new() { ["Version"] = "1.0" }),
   CreateMefDesc<IPlugin, PluginB>(metadata: new() { ["Version"] = "2.0" }),
   CreateMefDesc<IPlugin, PluginA>(metadata: new() { ["Version"] = "3.0" })
   ]);
  var plugA = sp.GetExportedValue(typeof(IPlugin), "PluginA");
  Assert.IsType<PluginA>(plugA);
  var many = sp.GetServices<ExportFactory<IPlugin, PluginMetadata>>().ToArray();
  Assert.Equal(2, many.Length);
  var v2 = many.Where(x => x.Metadata.Version == "2.0").ToArray();
  Assert.Single(v2);
  Assert.IsType<PluginB>(v2[0].CreateExport().Value);
 }

 [Fact]
 public void _8()
 {
  var sc = new ServiceCollection();
  sc.AddSingleton(new Configuration() { IsRelease = true });
  sc.AddTransient<IDatabaseConnection>(sp =>
  {
   var config = sp.GetService<Configuration>();
   return new DatabaseConnection()
   {
    DataSource = config.IsRelease ? "release" : "dev"
   };
  });
  var sp = ServiceResolverApiExtensions.BuildServiceProvider(sc, []);
  var db = sp.GetService<IDatabaseConnection>();
  var dbc = Assert.IsType<DatabaseConnection>(db);
  Assert.Equal("release", dbc.DataSource);
  var config = sp.GetService<Configuration>();
  config.IsRelease = false;
  db = sp.GetService<IDatabaseConnection>();
  dbc = Assert.IsType<DatabaseConnection>(db);
  Assert.Equal("dev", dbc.DataSource);
 }

 [Fact]
 public void _9()
 {
  var sc = new ServiceCollection();
  sc.AddTransient<IHeavyService, HeavyService>();
  sc.AddTransient<ConsumerService>();
  var sp = ServiceResolverApiExtensions.BuildServiceProvider(sc, []);
  var c = sp.GetService<ConsumerService>();
  Assert.NotNull(c.HeavyService);
  Assert.False(c.HeavyService.IsValueCreated);
  var firstCall = Assert.IsType<HeavyService>(c.HeavyService.Value);
  Assert.True(c.HeavyService.IsValueCreated);
  Assert.Equal(firstCall, c.HeavyService.Value);
 }

 [Fact]
 public void _10()
 {
  var sc = new ServiceCollection();
  sc.AddScoped<IResourceService, ResourceService>();
  sc.AddTransient<UsingScopedService>();
  var sp = ServiceResolverApiExtensions.BuildServiceProvider(sc, []);
  var scope = sp.CreateScope();
  var scopeProvider = scope.ServiceProvider;
  var rs = scopeProvider.GetService<IResourceService>();
  Assert.IsType<ResourceService>(rs);
  Assert.Equal(0, rs.DisposedCount);
  var us = scopeProvider.GetService<UsingScopedService>();
  Assert.Equal(rs, us.ResourceService);
  scope.Dispose();
  Assert.Equal(1, rs.DisposedCount);
  Assert.Equal(1, us.ResourceService.DisposedCount);
 }

 [Fact]
 public void _11()
 {
  var sc = new ServiceCollection();
  sc.AddTransient<ServiceThatInjectNonRegistered>();
  var sp = ServiceResolverApiExtensions.BuildServiceProvider(sc, []);
  var service = sp.GetService(typeof(INonRegisteredService));
  Assert.Null(service);
  service = sp.GetService<ServiceThatInjectNonRegistered>();
  var s1 = Assert.IsType<ServiceThatInjectNonRegistered>(service);
  Assert.Null(s1.NonRegisteredService);
 }

 [Fact]
 public void _12()
 {
  Assert.DoesNotContain(AssemblyLoadContext.Default.Assemblies, x => x.GetName().Name == "PluginLib");
  var data = new PrecomputedServiceDescriptionData(
    implementationType: "PluginLib.PluginFromLibImpl, PluginLib",
    keyLikeContract: null,
    contractType: "PluginLib.IPluginFromLib, PluginLib",
    creationPolicy: HbServiceLifetime.Transient,
    customAttributeType: null,
    customAttributeCtorArgsCreator: null,
    metadata: null,
    tag: "Tag"
   );
  var sp = ServiceResolverApiExtensions.BuildServiceProvider(new ServiceCollection(), [data]);
  Assert.DoesNotContain(AssemblyLoadContext.Default.Assemblies, x => x.GetName().Name == "PluginLib");
  var sd = sp.GetCurrentlyRegisteredServiceDescriptions().Last();
  var contract = sd.LoadServiceContract();
  var service = sp.GetService(contract);
  Assert.NotNull(service);
  Assert.Contains(AssemblyLoadContext.Default.Assemblies, x => x.GetName().Name == "PluginLib");
 }

 [Fact]
 public void _13()
 {
  var sp = ServiceResolverApiExtensions.BuildServiceProvider(new ServiceCollection(), [
   CreateMefDesc<ILogger, ConsoleLogger>()
   ]);
  var service = sp.GetService(typeof(ILogger));
  Assert.IsType<ConsoleLogger>(service);
  sp.Depopulate("Tag");
  sp.ClearAllBuildCaches();
  service = sp.GetService(typeof(ILogger));
  Assert.Null(service);
  sp.Populate([CreateMefDesc<ILogger, FileLogger>()]);
  sp.ClearAllBuildCaches();
  service = sp.GetService(typeof(ILogger));
  Assert.IsType<FileLogger>(service);
 }

 [Fact]
 public void _15()
 {
  var sc = new ServiceCollection().AddConnectionWide<UserSession>().PromiseToAddScoped<UserPreferences>();
  var sp = ServiceResolverApiExtensions.BuildServiceProvider(sc, []);
  var scope1 = sp.CreateScope().ServiceProvider;
  var scope2 = scope1.CreateScope().ServiceProvider;
  var session1 = scope1.GetService(typeof(UserSession));
  Assert.IsType<UserSession>(session1);
  var session2 = scope2.GetService(typeof(UserSession));
  Assert.Same(session1, session2);
  Assert.Throws<InvalidOperationException>(() => scope1.GetService(typeof(UserPreferences)));
  UserPreferences userPreferences = new();
  ((IConnectionWideCache)scope1).StoreObj(userPreferences);
  var up1 = Assert.IsType<UserPreferences>(scope1.GetService(typeof(UserPreferences)));
  Assert.Same(userPreferences, up1);
  var up2 = Assert.IsType<UserPreferences>(scope2.GetService(typeof(UserPreferences)));
  Assert.Same(userPreferences, up2);
 }

 public class UserSession;
 public class UserPreferences;

 interface INonRegisteredService;
 class ServiceThatInjectNonRegistered(INonRegisteredService nonRegisteredService)
 {
  public INonRegisteredService NonRegisteredService { get; } = nonRegisteredService;
 }

 interface IResourceService
 {
  int DisposedCount { get; }
 }
 class ResourceService : IResourceService, IDisposable
 {
  public int DisposedCount { get; set; }
  public void Dispose()
  {
   DisposedCount++;
  }
 }
 class UsingScopedService(IResourceService resourceService)
 {
  public IResourceService ResourceService { get; } = resourceService;
 }

 interface IHeavyService;
 class HeavyService : IHeavyService;
 class ConsumerService(Lazy<IHeavyService> heavyService)
 {
  public Lazy<IHeavyService> HeavyService { get; } = heavyService;
 }

 class Configuration
 {
  public bool IsRelease;
 }
 interface IDatabaseConnection;
 class DatabaseConnection : IDatabaseConnection
 {
  public string DataSource { get; set; }
 }

 interface IPlugin;
 class PluginMetadata
 {
  public string Version { get; set; }
 }
 class PluginA : IPlugin;
 class PluginB : IPlugin;

 interface ILogger;
 class ConsoleLogger : ILogger;
 class FileLogger : ILogger;

 class User;
 interface IRepository<T>;
 class Repository<T>;

 interface IService;
 class Service : IService;
 
 interface IService2;
 class Service2 : IService2;

 interface IService3;
 class Service3 : IService3;

 class DummyServiceDescription : IServiceDescription
 {
  public string? Tag { get; }
  public bool HasMetadata { get; }
  public bool IsPromiseToAddServiceDescriptor { get; }
  public bool IsImplementationFactory { get; }
  public bool IsImplementationInstance { get; }
  public bool IsImplementationType { get; }
  public bool IsEnumerableType { get; }

  public BaseServiceKey GetBaseServiceKey()
  {
   throw new NotImplementedException();
  }

  public (IEnumerable<IServiceDescription>, Type elementContractType, Type? requestedCollectionType) GetEnumerableDescription()
  {
   throw new NotImplementedException();
  }

  public Dictionary<string, object?> GetMetadata()
  {
   throw new NotImplementedException();
  }

  public HbServiceLifetime GetServiceScope()
  {
   throw new NotImplementedException();
  }

  public bool IsSameContractType(Type type)
  {
   throw new NotImplementedException();
  }

  public Func<IServiceProvider, object?> LoadFactory()
  {
   throw new NotImplementedException();
  }

  public object LoadImplementationInstance()
  {
   throw new NotImplementedException();
  }

  public Type LoadImplementationType()
  {
   throw new NotImplementedException();
  }

  public Type LoadServiceContract()
  {
   throw new NotImplementedException();
  }

  public bool SatisfiesStringContract(string? contract)
  {
   throw new NotImplementedException();
  }
 }
}