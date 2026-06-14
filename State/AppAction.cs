using DataViewer.Services;
using System.Collections.Generic;

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
public sealed record DismissToast : AppAction;
public sealed record ThemeChanged(ThemeMode Value) : AppAction;
public sealed record CompletionItemsChanged(IReadOnlyList<string> Items) : AppAction;
public sealed record CompletionAccepted(string SqlText) : AppAction;
