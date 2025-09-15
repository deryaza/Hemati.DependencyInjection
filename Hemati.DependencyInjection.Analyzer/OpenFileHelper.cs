// SPDX-License-Identifier: LGPL-3.0-only

using System.Diagnostics;

namespace Hemati.DependencyInjection.Analyzer;

internal static class OpenFileHelper
{
    public static int RetriesCount = 10;

    public static FileStream OpenWrite(string path)
    {
        int retryNo = 0;
        while (true)
        {
            try
            {
                return File.OpenWrite(masterFilePath);
            }
            catch when (retryNo++ < RetriesCount)
            {
                Debug.WriteLine($"Retrying to open file {path}");
            }
        }
    }
}
