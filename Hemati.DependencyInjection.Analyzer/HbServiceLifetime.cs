// SPDX-License-Identifier: LGPL-3.0-only

namespace Hemati.DependencyInjection.Implementation.Core;

public enum HbServiceLifetime
{
 Singleton = 0,
 Scoped = 1,
 Transient = 2,
 ConnectionWide = 3,
 ConnectionCache = 4
}