// SPDX-License-Identifier: LGPL-3.0-only

namespace Hemati.DependencyInjection.Analyzer;

public interface IExporter
{
    void Export(Dictionary<string, OneOf> allTypesThatExportSomething);

    string ExportPath { get; set; }
}

