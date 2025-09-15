// SPDX-License-Identifier: LGPL-3.0-only

namespace Hemati.DependencyInjection.Analyzer;

public class FIleExporter : IExporter
{
    public string ExportPath { get; set; } = null!;

    public void Export(Dictionary<string, OneOf> allTypesThatExportSomething)
    {
        string masterFilePath = Path.Combine(ExportPath, "di_registrations.cache");

        using FileStream matserFile = OpenFileHelper.OpenWrite(masterFilePath);
        BinaryWriter writer = new(masterFile);

        foreach (var kv in allTypesThatExportSomething)
        {
            (string uniqueFileName, OneOf oneOf) = (kv.Key, kv.Value);
            
        }

    }

    static void WriteForOne(BinaryWriter stream, TypeDiInfo diInfo)
    {
    }
}
