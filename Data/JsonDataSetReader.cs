using System;
using System.IO;
using System.Linq;

namespace DataViewer.Data;

public sealed class JsonDataSetReader : IDataSetReader
{
    private static readonly string[] Extensions = [".json", ".jsonl", ".ndjson"];

    public bool CanRead(string path) =>
        Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    public string CreateRelationSql(string path, string alias) =>
        $"CREATE OR REPLACE VIEW {Sql.Identifier(alias)} AS SELECT * FROM read_json_auto({Sql.Literal(path)});";

    public string GetPreviewSql(string alias, int limit) =>
        $"SELECT * FROM {Sql.Identifier(alias)} LIMIT {limit};";

    public string GetSchemaSql(string alias) =>
        $"DESCRIBE SELECT * FROM {Sql.Identifier(alias)};";
}
