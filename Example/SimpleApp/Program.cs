// SPDX-License-Identifier: LGPL-3.0-only

using System.Reflection;
using Hemati.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

var descriptions = ServiceResolverApiExtensions.LoadDescriptions();
var provider = ServiceResolverApiExtensions.BuildServiceProvider(new ServiceCollection(), descriptions);

var test = provider.GetService<Test>();

Console.WriteLine(test);

