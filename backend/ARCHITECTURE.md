# 后端分层架构

> 元数据驱动单表查询管理系统 · .NET 10 / ASP.NET Core

## 1. 设计目标

- **模块化**：抽象契约、数据库方言实现、业务逻辑、HTTP 接口、启动宿主五层分离，各自独立编译为 DLL，可单独复用；宿主可组合多个业务模块统一启动。
- **依赖单向**：`Abstractions ← Providers ← Core ← Web ← Host`，严禁逆向或循环引用。
- **零驱动耦合**：核心模块与抽象层不引用任何具体数据库驱动（MySqlConnector / Npgsql / …），驱动由宿主按需注册。
- **命名空间规范**：`项目名称.文件夹`，如 `MetaData.Abstractions.Enums`、`MetaData.Core.Services`。

## 2. 模块总览

```
┌─────────────────────────────────────────────────────┐
│  MetaData.Host  （启动宿主 / 配置与装配）            │
│  Program.cs · CORS · Swagger · 静态文件 · 驱动注册   │
└──────────────────────┬──────────────────────────────┘
                       │ 引用
┌──────────────────────▼──────────────────────────────┐
│  MetaData.Web  （HTTP 接口层 / 类库）                │
│  Controllers · MetaDataWebServiceCollectionExtensions│
└──────────────────────┬──────────────────────────────┘
                       │ 引用
┌──────────────────────▼──────────────────────────────┐
│  MetaData.Core  （业务层）                           │
│  Services · Data · Entities · Models · Security     │
│  MetaDataServiceCollectionExtensions                │
└──────────────────────┬──────────────────────────────┘
                       │ 引用
┌──────────────────────▼──────────────────────────────┐
│  MetaData.Providers  （数据库方言实现）              │
│  Dialects · SchemaInspection                        │
└──────────────────────┬──────────────────────────────┘
                       │ 引用
┌──────────────────────▼──────────────────────────────┐
│  MetaData.Abstractions  （抽象契约层）               │
│  根目录接口 · Enums · Exceptions · Schema            │
└─────────────────────────────────────────────────────┘
```

### 依赖方向

| 模块 | 引用 | 被引用 |
|---|---|---|
| `MetaData.Abstractions` | 无（零依赖） | Providers, Core, Web, Host |
| `MetaData.Providers` | Abstractions | Core |
| `MetaData.Core` | Abstractions + Providers | Web |
| `MetaData.Web` | Core（类库，含 HTTP 接口） | Host |
| `MetaData.Host` | Web（SDK.Web，唯一启动点） | — |

## 3. 各模块职责

### 3.1 MetaData.Abstractions — 抽象契约层

> **零依赖**。定义跨模块共享的接口、枚举、异常与 Schema 契约。可独立打包为 NuGet 供其他项目引用。

| 命名空间 | 内容 |
|---|---|
| `MetaData.Abstractions` | `IDatabaseDialect`、`IDbProviderRegistry`、`IDialectRegistry`、`ISchemaInspectorRegistry`、`ICurrentUser`、`ISecretProtector`、`DatabaseProviderRegistration`、`ConnectionSettings`、`DbVersionInfo`、`QueryParts`、`FilterCondition`、`SortItem`、`BuiltSql` |
| `MetaData.Abstractions.Enums` | `DatabaseType`、`ConnectionInputMode`、`AuthMode`、`OracleTargetType`、`DataCategory`、`FilterOperator`、`SortDirection` |
| `MetaData.Abstractions.Exceptions` | `MetaDataException`、`DriverNotRegisteredException` |
| `MetaData.Abstractions.Schema` | `ISchemaInspector`、`ITableSchemaSample`、`IColumnSchemaSample` |

**为什么不含业务 DTO**：DTO 与具体业务场景绑定，属于业务层契约而非跨模块抽象。放在抽象层会让纯引用抽象的模块被迫依赖业务模型，破坏复用边界。

### 3.2 MetaData.Providers — 数据库方言实现层

> 依赖 Abstractions。包含 5 种数据库的方言与结构读取器，不引用任何具体 ADO.NET 驱动包（驱动由宿主注册）。

| 命名空间 | 内容 |
|---|---|
| `MetaData.Providers.Dialects` | `DatabaseDialectBase`（公共 WHERE/ORDER BY/分页/连接串）、5 方言实现（MySql/PostgreSql/SqlServer/Db2/Oracle） |
| `MetaData.Providers.Dialects.Building` | `SqlBuildContext`（internal，SQL 参数命名与收集） |
| `MetaData.Providers.Registries` | `DialectRegistry`、`DbProviderRegistry`、`SchemaInspectorRegistry` |
| `MetaData.Providers.SchemaInspection` | `SchemaInspectorBase`、5 读取器实现、`ReaderExtensions`、`TableSchemaSample`/`ColumnSchemaSample`（internal 默认实现） |

**分页版本分支**：MySQL/PostgreSQL 用 LIMIT OFFSET；SQL Server ≥2012、Oracle ≥12、DB2 ≥10.1 用 OFFSET FETCH；旧版本回退 ROW_NUMBER / ROWNUM 包装。

### 3.3 MetaData.Core — 业务层

> 依赖 Abstractions + Providers。承载实体、EF DbContext、业务服务、DTO、DI 扩展。不含任何 Controller 或 HTTP 管道装配；异常处理不内置，由宿主自行配置。

| 命名空间 | 内容 |
|---|---|
| `MetaData.Core.Entities` | `DbConnectionInfo`、`TableMetadata`、`FieldMetadata`、`UserFieldPreference`（EF 实体） |
| `MetaData.Core.Data` | `IMetaDataDbContext`（模块数据访问契约，4 个 DbSet + SaveChangesAsync）、`MetaDataModelBuilderExtensions.ConfigureMetaData`（表结构注册扩展）；模块不提供 DbContext |
| `MetaData.Core.Models` | 业务 DTO：`ConnectionDto`、`TableDto`、`FieldDto`、`DataQueryRequest`/`Response`、`FieldPreferenceDto` 等 24 个 |
| `MetaData.Core.Services` | `ConnectionService`、`MetadataService`、`MetadataImportService`、`DataQueryService`、`UserPreferenceService`、`DbConnectionFactory`、`DefaultCurrentUser`、`MetaDataOptions`、`QueryOperatorMap` |
| `MetaData.Core.Security` | `DataProtectionSecretProtector` |
| `MetaData.Core` | `MetaDataServiceCollectionExtensions`（`AddMetaDataCore` 注册业务服务、方言、注册表） |

### 3.4 MetaData.Web — HTTP 接口层

> 类库（`Microsoft.NET.Sdk` + `FrameworkReference Microsoft.AspNetCore.App`），依赖 Core。只承载 HTTP 入站适配器，不含启动代码与宿主配置。

| 命名空间/文件 | 职责 |
|---|---|
| `MetaData.Web.Controllers` | `ConnectionsController`、`MetadataController`、`DataController`、`MetaController`（仅 GET/POST，GET 仅用于无参请求） |
| `MetaDataWebServiceCollectionExtensions` | `AddMetaDataWeb`：`AddControllers().AddApplicationPart(本程序集)` + JSON 枚举字符串 |

### 3.5 MetaData.Host — 启动宿主

> `Microsoft.NET.Sdk.Web`，唯一启动点。仅做配置与模块装配：定义多模块共享的集成 DbContext、EF Core 提供程序、Swagger、CORS、静态文件、按需注册 ADO.NET 驱动、异常处理中间件。

| 文件 | 职责 |
|---|---|
| `Program.cs` | `AddDbContext<AppDbContext>(...)` + 接口映射、`AddMetaDataCore()` + `AddMetaDataWeb()`、`AddDatabaseProvider(...)`、`MetaDataException` 转 400 JSON、EnsureCreated、Swagger、静态文件 + SPA 回退 |
| `Data/AppDbContext.cs` | 宿主集成 DbContext，实现 `IMetaDataDbContext`，OnModelCreating 调用 `ConfigureMetaData()`；新增模块时在此实现其 I*DbContext 并追加 Configure* |
| `appsettings*.json` | 宿主配置 |
| `MetaData.Host.csproj` | 引用 Web + Sqlite + Swashbuckle |
| `wwwroot/` | Angular 构建产物 |

**多模块组合**：新增业务模块时，在本项目引用其 Web（HttpApi）项目并调用对应的注册扩展即可，无需改动既有模块。

## 4. 关键设计约束

### 4.1 驱动零耦合
`MetaData.Abstractions`、`Providers`、`Core`、`Web` 均不引用任何 ADO.NET 驱动 NuGet 包。宿主通过 `AddDatabaseProvider(DatabaseType.MySql, MySqlConnectorFactory.Instance)` 按需注册，未注册时返回中文 400 引导。

### 4.2 一类一文件
每个 `.cs` 文件只包含一个顶层类型，文件名即类型名（详见 `.trae/rules/csharp-one-type-per-file.md`）。

### 4.3 仅 GET/POST，参数统一 POST + JSON
HTTP 接口不使用 PUT/DELETE/PATCH，不使用路由参数。GET 仅用于无参查询（如 `list`、`operators`、`published-tables`）；凡需要传参的请求统一使用 POST + JSON body，不使用 query string（文件上传等 multipart 场景除外）。

### 4.4 暂不鉴权
`ICurrentUser` 在 Abstractions 中定义，`DefaultCurrentUser` 在 Core 中读 `MetaData:DefaultUserId`（默认 `default`）。用户偏好按 UserId 隔离。

### 4.5 EF 管元数据 / ADO 查业务
元数据（连接/表/字段/偏好）用 EF Core 管理；业务库数据查询用 ADO.NET，由方言接口生成方言化 SQL（分页、参数前缀、引用符）。

## 5. 扩展指南

| 场景 | 改动位置 |
|---|---|
| 新增数据库支持 | `Providers/Dialects/` + `Providers/SchemaInspection/`，宿主注册驱动 |
| 新增业务接口 | `Web/Controllers/` + `Core/Services/` + `Core/Models/` DTO |
| 新增元数据实体 | `Core/Entities/` + 在 `ConfigureMetaData` 中补映射 + `IMetaDataDbContext` 补 DbSet |
| 接入鉴权 | Core 中实现 `ICurrentUser`，替换 `DefaultCurrentUser` 注册 |
| 替换元数据库 | 宿主 `Program.cs` 改 EF 提供程序（如 `UseNpgsql`） |
| 前端独立引用 DTO | 抽象层 Enums/Exceptions 可引用；业务 DTO 在 Core 中，按需引用 Core 或抽到共享契约项目 |

## 6. 目录结构

```
backend/
├── MetaData.slnx                # .NET 10 解决方案（新格式）
├── ARCHITECTURE.md              # 本文档
├── src/
│   ├── MetaData.Abstractions/   # 抽象契约层（零依赖）
│   │   ├── Enums/               # 7 枚举
│   │   ├── Exceptions/           # MetaDataException, DriverNotRegisteredException
│   │   ├── Schema/              # ISchemaInspector, ITableSchemaSample, IColumnSchemaSample
│   │   └── *.cs                 # IDatabaseDialect, IDbProviderRegistry, ICurrentUser, ...
│   ├── MetaData.Providers/      # 数据库方言实现
│   │   ├── Dialects/            # DatabaseDialectBase, 5 方言
│   │   │   └── Building/        # SqlBuildContext (internal)
│   │   ├── Registries/          # DialectRegistry, DbProviderRegistry, SchemaInspectorRegistry
│   │   └── SchemaInspection/    # SchemaInspectorBase, 5 读取器, 样本默认实现
│   ├── MetaData.Core/           # 业务层
│   │   ├── Data/                # IMetaDataDbContext + ConfigureMetaData 扩展
│   │   ├── Entities/            # 4 EF 实体
│   │   ├── Models/              # 24 业务 DTO
│   │   ├── Security/            # DataProtectionSecretProtector
│   │   ├── Services/            # 9 服务 + IDbConnectionFactory + MetaDataOptions
│   │   └── MetaDataServiceCollectionExtensions.cs
│   ├── MetaData.Web/            # HTTP 接口层（类库）
│   │   ├── Controllers/         # 4 Controller
│   │   └── MetaDataWebServiceCollectionExtensions.cs
│   └── MetaData.Host/           # 启动宿主（唯一启动点）
│       ├── Data/                # AppDbContext（集成 DbContext）
│       ├── Program.cs
│       ├── Properties/
│       ├── appsettings*.json
│       └── wwwroot/             # Angular 构建产物
└── tests/
    └── MetaData.Core.Tests/     # xUnit（46 测试）
```
