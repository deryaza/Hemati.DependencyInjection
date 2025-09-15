// SPDX-License-Identifier: LGPL-3.0-only

namespace TestProject1;

public static class Ex
{
    public static void DoesNotThrow(Action a)
    {
        Exception? ex = Record.Exception(a);
        Assert.Null(ex);
    }
}