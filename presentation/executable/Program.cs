// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using lib;

var descriptions = ServiceResolverApiExtensions.LoadDescriptions();
var provider = ServiceResolverApiExtensions.BuildServiceProvider(new ServiceCollection(), descriptions);

var service = provider.GetService<ILibService>();
service.Do();
