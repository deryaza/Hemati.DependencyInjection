// SPDX-License-Identifier: LGPL-3.0-only

using System.ComponentModel.Composition;

using Hemati.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;

namespace TestProject1;

public class DependentPropertiesSetter
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
 public void BasicImportTest()
 {
  var sp = Setup(x => x.AddTransient<Test>().AddTransient<Test2>());
  Test? service = sp.GetService<Test>();
  Assert.NotNull(service);
  Assert.NotNull(service.Imported);
 }

 [Fact]
 public void InInheritedImportTest()
 {
  var sp = Setup(x => x.AddTransient<Test>().AddTransient<Test2>().AddTransient<InheritsTest>());
  Test? service = sp.GetService<InheritsTest>();
  Assert.NotNull(service);
  Assert.NotNull(service.Imported);
 }

 [Fact]
 public void WithPrivateSetterImportTest()
 {
  var sp = Setup(x => x.AddTransient<Test3>().AddTransient<Test2>().AddTransient<InheritsTest>());
  Test3? service = sp.GetService<Test3>();
  Assert.NotNull(service);
  Assert.NotNull(service.Imported);
 }

 class Test3
 {
  [Import] public Test2 Imported { get; private set; }
 }

 class InheritsTest : Test
 {
 }

 class Test
 {
  [Import] public Test2 Imported { get; set; }
 }

 class Test2
 {
 }
}