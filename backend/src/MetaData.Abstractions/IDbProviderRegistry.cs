using System.Data.Common;
using MetaData.Abstractions.Enums;

namespace MetaData.Abstractions;

/// <summary>
/// ADO.NET 驱动注册表。具体 DbProviderFactory 由宿主启动项目注册，核心模块不引用任何驱动。
/// </summary>
public interface IDbProviderRegistry
{
    void Register(DatabaseType databaseType, DbProviderFactory factory);

    /// <summary>解析驱动；未注册时抛出 DriverNotRegisteredException（中文友好提示）。</summary>
    DbProviderFactory Resolve(DatabaseType databaseType);

    bool IsRegistered(DatabaseType databaseType);
}
