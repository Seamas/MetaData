using MetaData.Abstractions;
using MetaData.Abstractions.Enums;
using MetaData.Abstractions.Exceptions;
using MetaData.Abstractions.Schema;

namespace MetaData.Providers.Registries;

/// <summary>按数据库类型解析结构读取器。</summary>
public class SchemaInspectorRegistry : ISchemaInspectorRegistry
{
    private readonly Dictionary<DatabaseType, ISchemaInspector> _inspectors;

    public SchemaInspectorRegistry(IEnumerable<ISchemaInspector> inspectors)
    {
        _inspectors = inspectors.ToDictionary(i => i.DatabaseType);
    }

    public ISchemaInspector Resolve(DatabaseType databaseType)
    {
        if (!_inspectors.TryGetValue(databaseType, out var inspector))
        {
            throw new MetaDataException($"不支持的数据库类型：{databaseType}");
        }

        return inspector;
    }
}
