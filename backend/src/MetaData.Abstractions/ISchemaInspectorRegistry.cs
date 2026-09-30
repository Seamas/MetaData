using MetaData.Abstractions.Enums;
using MetaData.Abstractions.Schema;

namespace MetaData.Abstractions;

/// <summary>按数据库类型解析结构读取器。</summary>
public interface ISchemaInspectorRegistry
{
    ISchemaInspector Resolve(DatabaseType databaseType);
}
