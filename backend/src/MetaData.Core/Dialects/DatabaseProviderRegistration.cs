using System.Data.Common;
using MetaData.Core.Enums;

namespace MetaData.Core.Dialects;

/// <summary>宿主注册的一个具体驱动（注册项）。</summary>
public sealed record DatabaseProviderRegistration(DatabaseType DatabaseType, DbProviderFactory Factory);
