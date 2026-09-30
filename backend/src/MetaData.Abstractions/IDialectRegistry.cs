using MetaData.Abstractions;
using MetaData.Abstractions.Enums;

namespace MetaData.Abstractions;

/// <summary>按数据库类型解析方言。</summary>
public interface IDialectRegistry
{
    IDatabaseDialect Resolve(DatabaseType databaseType);
}
