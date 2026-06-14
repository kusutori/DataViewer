using DataViewer.Services;
using System.Collections.Generic;

namespace DataViewer.State;

public sealed record AppState(
    LoadedDataSet? DataSet,
    string SqlText,
    QueryResult? Result,
    bool IsBusy,
    string? ErrorMessage,
    ThemeMode ThemeMode,
    IReadOnlyList<string> CompletionItems)
{
    public static AppState Initial { get; } = new(
        DataSet: null,
        SqlText: "",
        Result: null,
        IsBusy: false,
        ErrorMessage: null,
        ThemeMode: ThemeMode.System,
        CompletionItems: []);
}

public enum ThemeMode
{
    System,
    Light,
    Dark,
}
