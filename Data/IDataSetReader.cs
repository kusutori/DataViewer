using System.Collections.Generic;

namespace DataViewer.Data;

public interface IDataSetReader
{
    bool CanRead(string path);
    string CreateRelationSql(string path, string alias);
    string GetPreviewSql(string alias, int limit);
    string GetSchemaSql(string alias);
}
