namespace DataViewer.Services;

public sealed record LoadedDataSet(
    string Path,
    string FileName,
    string Alias,
    string Format,
    int PreviewLimit);
