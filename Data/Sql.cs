using System;

namespace DataViewer.Data;

internal static class Sql
{
    public static string Identifier(string value) =>
        "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    public static string Literal(string value) =>
        "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
}
