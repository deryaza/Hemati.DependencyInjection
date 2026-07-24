// SPDX-License-Identifier: LGPL-3.0-only

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Hemati.DependencyInjection;
using Hemati.DependencyInjection.Serialization;
using Microsoft.Extensions.DependencyInjection;

var path = Path.Combine(AppContext.BaseDirectory, "SimpleApp.Services.precomputed");
var serviceFilesAll = Enumerable.Repeat(path, 1000).ToArray();

[MethodImpl(MethodImplOptions.NoInlining)]
PrecomputedServiceDescriptionData[] Loader()
{
    return PrecomputedDataLoader.Load(serviceFilesAll);
}

Stopwatch startNew = Stopwatch.StartNew();
var descriptions = Loader();
startNew.Stop();
Console.WriteLine(startNew.Elapsed.TotalMilliseconds);

// var provider = ServiceResolverApiExtensions.BuildServiceProvider(new ServiceCollection(), descriptions);
// var test = provider.GetService<Test>();
// Console.WriteLine(test);