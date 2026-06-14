using DataViewer.Services;

namespace DataViewer.State;

public sealed record AppState(
    LoadedDataSet? DataSet,
    string SqlText,
    QueryResult? Result,
    bool IsBusy,
    string? ErrorMessage)
{
    public static AppState Initial { get; } = new(
        DataSet: null,
        SqlText: "",
        Result: null,
        IsBusy: false,
        ErrorMessage: null);
}
