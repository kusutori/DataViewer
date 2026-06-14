using System;
using System.IO;

namespace DataViewer.Data;

public sealed class CsvDataSetReader : IDataSetReader
{
    public bool CanRead(string path) =>
        string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase);

    public string CreateRelationSql(string path, string alias) =>
        $"CREATE OR REPLACE VIEW {Sql.Identifier(alias)} AS SELECT * FROM read_csv_auto({Sql.Literal(path)});";

    public string GetPreviewSql(string alias, int limit) =>
        $"SELECT * FROM {Sql.Identifier(alias)} LIMIT {limit};";

    public string GetSchemaSql(string alias) =>
        $"DESCRIBE SELECT * FROM {Sql.Identifier(alias)};";
}
