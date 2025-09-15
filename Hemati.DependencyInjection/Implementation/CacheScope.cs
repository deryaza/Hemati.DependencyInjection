// SPDX-License-Identifier: LGPL-3.0-only

namespace Hemati.DependencyInjection.Implementation;

[Flags]
public enum CacheScope
{
 // read: not activated
 None = 0,

 // read: activated in
 Singleton = 0b1,
 Scoped = 0b10,
 Transient = 0b100,
 ConnectionWide = 0b1000,
 ConnectionCache = 0b10000
}

public enum ScopeRole
{
 /// <summary>
 /// Root scope means that this scope represents ServiceResolver itself
 /// </summary>
 RootScope,

 /// <summary>
 /// Parent scope means that this scope was created from ServiceResolver
 /// </summary>
 ParentScope,

 /// <summary>
 /// Parent scope means that this scope was created from other scope
 /// </summary>
 ChildScope
}