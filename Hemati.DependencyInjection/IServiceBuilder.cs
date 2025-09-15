// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection.Implementation;
using Hemati.DependencyInjection.Implementation.Parameters;

namespace Hemati.DependencyInjection;

public interface IServiceBuilder
{
 Func<ScopeCache, IServiceProvider, object?> Build(Parameter parameter);
}

