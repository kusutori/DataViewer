using System;
using System.IO;

namespace DataViewer.Data;

public sealed class ParquetDataSetReader : IDataSetReader
{
    public bool CanRead(string path) =>
        string.Equals(Path.GetExtension(path), ".parquet", StringComparison.OrdinalIgnoreCase);

    public string CreateRelationSql(string path, string alias) =>
        $"CREATE OR REPLACE VIEW {Sql.Identifier(alias)} AS SELECT * FROM read_parquet({Sql.Literal(path)});";

    public string GetPreviewSql(string alias, int limit) =>
        $"SELECT * FROM {Sql.Identifier(alias)} LIMIT {limit};";

    public string GetSchemaSql(string alias) =>
        $"DESCRIBE SELECT * FROM {Sql.Identifier(alias)};";
}
