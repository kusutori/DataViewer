using System;
using System.Collections.Generic;
using System.Linq;

namespace DataViewer.Data;

public sealed class DataSetRegistry
{
    private readonly IReadOnlyList<IDataSetReader> readers =
    [
        new CsvDataSetReader(),
        new TsvDataSetReader(),
        new ParquetDataSetReader(),
        new JsonDataSetReader(),
    ];

    public IDataSetReader Resolve(string path) =>
        readers.FirstOrDefault(reader => reader.CanRead(path))
        ?? throw new NotSupportedException("目前支持 CSV、TSV、Parquet、JSON、JSONL 和 NDJSON 文件。");
}
