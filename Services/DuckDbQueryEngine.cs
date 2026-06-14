using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using DuckDB.NET.Data;
using DataViewer.Data;

namespace DataViewer.Services;

public sealed class DuckDbQueryEngine : IDisposable
{
    private const int PreviewLimit = 200;

    private readonly DuckDBConnection connection;
    private readonly DataSetRegistry registry = new();
    private int nextAliasId;

    public DuckDbQueryEngine()
    {
        connection = new DuckDBConnection("Data Source=:memory:");
        connection.Open();
    }

    public LoadedDataSet LoadDataSet(string path)
    {
        var reader = registry.Resolve(path);
        var alias = $"dataset_{++nextAliasId}";

        ExecuteNonQuery(reader.CreateRelationSql(path, alias));
        var columns = GetColumns(reader.GetSchemaSql(alias));

        return new LoadedDataSet(
            Path: path,
            FileName: Path.GetFileName(path),
            Alias: alias,
            Format: Path.GetExtension(path).TrimStart('.').ToUpperInvariant(),
            PreviewLimit: PreviewLimit,
            Columns: columns);
    }

    public string CreateDefaultSql(LoadedDataSet dataSet) =>
        $"SELECT * FROM {Sql.Identifier(dataSet.Alias)} LIMIT {dataSet.PreviewLimit};";

    public QueryResult Query(string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        var columns = Enumerable.Range(0, reader.FieldCount)
            .Select(reader.GetName)
            .ToArray();

        var rows = new List<IReadOnlyList<string?>>();
        while (reader.Read())
        {
            var values = new string?[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                values[i] = reader.IsDBNull(i) ? null : Convert.ToString(reader.GetValue(i));
            }

            rows.Add(values);
        }

        return new QueryResult(columns, rows, rows.Count);
    }

    private void ExecuteNonQuery(string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private IReadOnlyList<string> GetColumns(string schemaSql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = schemaSql;

        using var reader = command.ExecuteReader();
        var columns = new List<string>();
        while (reader.Read())
        {
            if (!reader.IsDBNull(0))
            {
                columns.Add(Convert.ToString(reader.GetValue(0)) ?? "");
            }
        }

        return columns.Where(column => !string.IsNullOrWhiteSpace(column)).ToArray();
    }

    public void Dispose() => connection.Dispose();
}
