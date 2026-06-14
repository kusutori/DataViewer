using DataViewer.Services;

namespace DataViewer.State;

public abstract record AppAction;

public sealed record OpenFileRequested : AppAction;
public sealed record FileLoaded(LoadedDataSet DataSet, string SqlText, QueryResult Result) : AppAction;
public sealed record LoadFailed(string Message) : AppAction;
public sealed record SqlChanged(string Value) : AppAction;
public sealed record RunQueryRequested : AppAction;
public sealed record QuerySucceeded(QueryResult Result) : AppAction;
public sealed record QueryFailed(string Message) : AppAction;
public sealed record ResetQuery(string SqlText, QueryResult Result) : AppAction;
