using DataViewer.Services;

namespace DataViewer.State;

public sealed record AppState(
    LoadedDataSet? DataSet,
    string SqlText,
    QueryResult? Result,
    bool IsBusy,
    string? ErrorMessage,
    ThemeMode ThemeMode,
    CodeEditorSettings EditorSettings,
    int SqlEditorSyncVersion)
{
    public static AppState Initial { get; } = new(
        DataSet: null,
        SqlText: "",
        Result: null,
        IsBusy: false,
        ErrorMessage: null,
        ThemeMode: ThemeMode.System,
        EditorSettings: CodeEditorSettings.Default,
        SqlEditorSyncVersion: 0);
}

public enum ThemeMode
{
    System,
    Light,
    Dark,
}

public sealed record CodeEditorSettings(
    CodeEditorThemeMode ThemeMode,
    string FontFamily,
    double FontSize,
    bool AcceptCompletionOnTab)
{
    public static CodeEditorSettings Default { get; } = new(
        ThemeMode: CodeEditorThemeMode.FollowApp,
        FontFamily: "Cascadia Code",
        FontSize: 13,
        AcceptCompletionOnTab: true);
}

public enum CodeEditorThemeMode
{
    FollowApp,
    Light,
    Dark,
}
