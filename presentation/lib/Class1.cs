// SPDX-License-Identifier: LGPL-3.0-only

using Hemati.DependencyInjection;

namespace lib;

public interface ILibService
{
    void Do();
}

[TransientImplementationOf(typeof(ILibService))]
public class Class1 : ILibService
{
    public void Do()
    {
        Console.WriteLine("Hello from lib!");
    }
}
