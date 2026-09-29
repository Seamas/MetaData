using MetaData.Core.Enums;

namespace MetaData.Core.Infrastructure;

/// <summary>业务库 ADO.NET 驱动未在宿主注册。</summary>
public class DriverNotRegisteredException : BusinessException
{
    public DriverNotRegisteredException(DatabaseType databaseType, string? packageHint)
        : base($"未注册 {databaseType} 的 ADO.NET 驱动。请在启动项目中引用对应 NuGet 包（{packageHint}）并调用 " +
               $"builder.Services.AddDatabaseProvider(DatabaseType.{databaseType}, XxxFactory.Instance) 完成注册。")
    {
    }
}
