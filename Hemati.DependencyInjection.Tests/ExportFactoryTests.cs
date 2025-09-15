// SPDX-License-Identifier: LGPL-3.0-only

using System.ComponentModel.Composition;
using Hemati.DependencyInjection;
using Hemati.DependencyInjection.Implementation;
using Hemati.DependencyInjection.Implementation.Core;
using Microsoft.Extensions.DependencyInjection;

namespace TestProject1;

public class ExportFactoryTests
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
    public void SimpleExportFactoryTest()
    {
        var sp = Setup(
            collection => collection.AddTransient<TypeThatImportsExports>(),
            list =>
            {
                list.Add(
                    new PrecomputedServiceDescriptionData(
                        implementationType: new BaseServiceKey(typeof(ExportFactoryType1), null).TypeName,
                        keyLikeContract: null,
                        contractType: null,
                        creationPolicy: HbServiceLifetime.Transient,
                        customAttributeType: new BaseServiceKey(typeof(ExportFactoryExportAttribute), null).TypeName,
                        customAttributeCtorArgsCreator: () => [1, "1", null],
                        metadata: null,
                        tag: "Tag"
                    )
                );
                list.Add(
                    new PrecomputedServiceDescriptionData(
                        implementationType: new BaseServiceKey(typeof(ExportFactoryType2), null).TypeName,
                        keyLikeContract: null,
                        contractType: null,
                        creationPolicy: HbServiceLifetime.Transient,
                        customAttributeType: new BaseServiceKey(typeof(ExportFactoryExportAttribute), null).TypeName,
                        customAttributeCtorArgsCreator: () => [2, "2", "222"],
                        metadata: null,
                        tag: "Tag"
                    )
                );
            });

        var services = sp.GetServices<ExportFactory<ICommonExportFactory, ExportFactoryExportAttrProxy>>();
        Assert.NotNull(services);

        ExportFactory<ICommonExportFactory, ExportFactoryExportAttrProxy>[] exportFactories = services.ToArray();
        Assert.Equal(exportFactories.Length, 2);

        var e1 = exportFactories[0];
        var e2 = exportFactories[1];

        Assert.Equal(e1.Metadata.Integer, 1);
        Assert.Equal(e1.Metadata.Str, "1");
        Assert.Equal(e1.Metadata.Obj, null);

        Assert.Equal(e2.Metadata.Integer, 2);
        Assert.Equal(e2.Metadata.Str, "2");
        Assert.Equal(e2.Metadata.Obj, "222");

        Assert.NotNull(e1.CreateExport().Value);
        Assert.NotNull(e2.CreateExport().Value);
    }

    [Fact]
    public void type_export_with_metadata()
    {
        var sp = Setup(
            collection => collection.AddTransient<TypeThatImportsExports>(),
            list =>
            {
                list.Add(
                    new PrecomputedServiceDescriptionData(
                        implementationType: new BaseServiceKey(typeof(ExportFactoryType1), null).TypeName,
                        keyLikeContract: null,
                        contractType: null,
                        creationPolicy: HbServiceLifetime.Transient,
                        customAttributeType: new BaseServiceKey(typeof(ExportFactoryExportAttribute), null).TypeName,
                        customAttributeCtorArgsCreator: () => [1, "1", null],
                        metadata: null,
                        tag: "Tag"
                    )
                );
                list.Add(
                    new PrecomputedServiceDescriptionData(
                        implementationType: new BaseServiceKey(typeof(ExportFactoryType2), null).TypeName,
                        keyLikeContract: null,
                        contractType: null,
                        creationPolicy: HbServiceLifetime.Transient,
                        customAttributeType: new BaseServiceKey(typeof(ExportFactoryExportAttribute), null).TypeName,
                        customAttributeCtorArgsCreator: () => [2, "2", "222"],
                        metadata: null,
                        tag: "Tag"
                    )
                );
            });

        var service = sp.GetService<TypeThatImportsExports>();
        Assert.NotNull(service);
        Assert.NotNull(service.ExportFactories);

        var exportFactories = service.ExportFactories.ToArray();
        Assert.Equal(exportFactories.Length, 2);

        var e1 = exportFactories[0];
        var e2 = exportFactories[1];

        Assert.Equal(e1.Metadata.Integer, 1);
        Assert.Equal(e1.Metadata.Str, "1");
        Assert.Null(e1.Metadata.Obj);

        Assert.Equal(e2.Metadata.Integer, 2);
        Assert.Equal(e2.Metadata.Str, "2");
        Assert.Equal(e2.Metadata.Obj, "222");

        Assert.NotNull(e1.CreateExport().Value);
        Assert.NotNull(e2.CreateExport().Value);
    }

    interface ICommonExportFactory
    {
    }

    class ExportFactoryType1 : ICommonExportFactory
    {
    }

    class ExportFactoryType2 : ICommonExportFactory
    {
    }


    class TypeThatImportsExports([ImportMany] IEnumerable<ExportFactory<ICommonExportFactory, ExportFactoryExportAttrProxy>> exportFactories)
    {
        public IEnumerable<ExportFactory<ICommonExportFactory, ExportFactoryExportAttrProxy>> ExportFactories { get; } = exportFactories;
    }

    class ExportFactoryExportAttribute(int integer, string str, object obj) : ExportAttribute(typeof(ICommonExportFactory))
    {
        public int Integer { get; set; } = integer;
        public string Str { get; set; } = str;
        public object Obj { get; set; } = obj;
    }

    class ExportFactoryExportAttrProxy
    {
        public int Integer { get; set; }

        public string? Str { get; set; }

        public object? Obj { get; set; }
    }
}