using System.Collections.Generic;

namespace DataViewer.Services;

public sealed record QueryResult(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string?>> Rows,
    int DisplayedRowCount);
