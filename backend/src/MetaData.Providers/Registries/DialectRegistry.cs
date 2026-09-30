using MetaData.Abstractions;
using MetaData.Abstractions.Enums;
using MetaData.Abstractions.Exceptions;

namespace MetaData.Providers.Registries;

/// <summary>基于已注册方言集合的默认注册表。</summary>
public class DialectRegistry : IDialectRegistry
{
    private readonly Dictionary<DatabaseType, IDatabaseDialect> _dialects;

    public DialectRegistry(IEnumerable<IDatabaseDialect> dialects)
    {
        _dialects = dialects.ToDictionary(d => d.DatabaseType);
    }

    public IDatabaseDialect Resolve(DatabaseType databaseType)
    {
        if (!_dialects.TryGetValue(databaseType, out var dialect))
        {
            throw new MetaDataException($"不支持的数据库类型：{databaseType}");
        }

        return dialect;
    }
}
