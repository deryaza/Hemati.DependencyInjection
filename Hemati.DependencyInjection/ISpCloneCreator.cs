// SPDX-License-Identifier: LGPL-3.0-only

using Microsoft.Extensions.DependencyInjection;

namespace Hemati.DependencyInjection;

public interface ISpCloneCreator
{
 IServiceProvider Clone(IEnumerable<ServiceDescriptor> descriptorsToReplace);
}