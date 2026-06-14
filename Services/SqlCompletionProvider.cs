using System;
using System.Collections.Generic;
using System.Linq;
using DataViewer.Data;
using DataViewer.State;

namespace DataViewer.Services;

public static class SqlCompletionProvider
{
    private static readonly string[] Keywords =
    [
        "SELECT", "FROM", "WHERE", "GROUP BY", "ORDER BY", "LIMIT", "OFFSET",
        "JOIN", "LEFT JOIN", "INNER JOIN", "ON", "AS", "AND", "OR", "NOT",
        "COUNT", "SUM", "AVG", "MIN", "MAX", "DISTINCT", "CASE", "WHEN", "THEN", "ELSE", "END",
    ];

    public static IReadOnlyList<string> GetSuggestions(AppState state)
    {
        var token = GetCurrentToken(state.SqlText);
        var candidates = new List<string>(Keywords);

        if (state.DataSet is not null)
        {
            candidates.Add(state.DataSet.Alias);
            candidates.AddRange(state.DataSet.Columns);
            candidates.AddRange(state.DataSet.Columns.Select(column => $"{state.DataSet.Alias}.{Sql.Identifier(column)}"));
        }

        return candidates
            .Where(candidate => token.Length == 0 || candidate.StartsWith(token, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();
    }

    public static string ApplySuggestion(string sql, string suggestion)
    {
        var index = sql.Length - 1;
        while (index >= 0 && IsTokenChar(sql[index]))
        {
            index--;
        }

        var prefix = sql[..(index + 1)];
        return prefix + suggestion + " ";
    }

    private static string GetCurrentToken(string sql)
    {
        var index = sql.Length - 1;
        while (index >= 0 && IsTokenChar(sql[index]))
        {
            index--;
        }

        return sql[(index + 1)..];
    }

    private static bool IsTokenChar(char value) =>
        char.IsLetterOrDigit(value) || value == '_' || value == '.';
}
