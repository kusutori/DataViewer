using System.Collections.Generic;

namespace DataViewer.Services;

public sealed record LoadedDataSet(
    string Path,
    string FileName,
    string Alias,
    string Format,
    int PreviewLimit,
    IReadOnlyList<string> Columns);
