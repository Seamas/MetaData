using MetaData.Core.Abstractions;
using MetaData.Core.Enums;

namespace MetaData.Core.Dialects;

/// <summary>按数据库类型解析方言。</summary>
public interface IDialectRegistry
{
    IDatabaseDialect Resolve(DatabaseType databaseType);
}
