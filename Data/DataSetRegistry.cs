using System;
using System.Collections.Generic;
using System.Linq;

namespace DataViewer.Data;

public sealed class DataSetRegistry
{
    private readonly IReadOnlyList<IDataSetReader> readers =
    [
        new CsvDataSetReader(),
        new ParquetDataSetReader(),
    ];

    public IDataSetReader Resolve(string path) =>
        readers.FirstOrDefault(reader => reader.CanRead(path))
        ?? throw new NotSupportedException("目前仅支持 CSV 和 Parquet 文件。");
}
