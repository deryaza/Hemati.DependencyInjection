// SPDX-License-Identifier: LGPL-3.0-only

using System.Runtime.CompilerServices;

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

public static class CacheScopeIndexes
{
    public const int Singleton = 0;
    public const int Scoped = 1;
    public const int ConnectionWide = 2;
    public const int ConnectionCache = 3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToIndex(CacheScope scope) => scope switch
    {
        CacheScope.Singleton => Singleton,
        CacheScope.Scoped => Scoped,
        CacheScope.ConnectionWide => ConnectionWide,
        CacheScope.ConnectionCache => ConnectionCache,
        _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null)
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CacheScope ToScope(int index) => index switch
    {
        Singleton => CacheScope.Singleton,
        Scoped => CacheScope.Scoped,
        ConnectionWide => CacheScope.ConnectionWide,
        ConnectionCache => CacheScope.ConnectionCache,
        _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
    };
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