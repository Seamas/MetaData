using MetaData.Core.Abstractions;
using MetaData.Core.Enums;
using MetaData.Core.Infrastructure;

namespace MetaData.Core.Dialects;

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
            throw new BusinessException($"不支持的数据库类型：{databaseType}");
        }

        return dialect;
    }
}
