# 后端分层架构

> 元数据驱动单表查询管理系统 · .NET 10 / ASP.NET Core

## 1. 设计目标

- **模块化**：抽象契约、数据库方言实现、领域、应用用例、基础设施、HTTP 接口、启动宿主七层分离，各自独立编译为 DLL，可单独复用；宿主可组合多个业务模块统一启动。
- **基座共享**：跨模块通用抽象（实体基类、泛型仓储、工作单元、分页契约、业务异常、事务特性）下沉在基座 `MyWebProject.Shared`，本模块通过跨目录项目引用原地复用，将来发布 NuGet 包。
- **依赖单向**：`Abstractions ← Providers ← Core ← Application ← Web ← Host`；`Infrastructure → Core/Abstractions/Providers` 提供实现，由 Host 装配。严禁逆向或循环引用。
- **零驱动耦合**：抽象层、领域层、应用层不引用任何具体数据库驱动（MySqlConnector / Npgsql / …），驱动由宿主按需注册。
- **命名空间规范**：`项目名称.文件夹`，如 `MetaData.Abstractions.Enums`、`MetaData.Core.Repositories`。

## 2. 模块总览

```
┌─────────────────────────────────────────────────────┐
│  MetaData.Host  （启动宿主 / 配置与装配）            │
│  Program.cs · AppDbContext（审计填充/事务）· 驱动注册│
└───────┬──────────────────────────────────┬──────────┘
        │ 引用                             │ 引用
┌───────▼──────────────┐         ┌─────────▼──────────────────────┐
│  MetaData.Web        │         │  MetaData.Infrastructure       │
│  HTTP 接口层（类库） │         │  仓储/UoW/连接工厂/数据保护/   │
│  Controllers         │         │  当前用户/方言与注册表装配     │
└───────┬──────────────┘         └─────────┬──────────────────────┘
        │ 引用                             │ 实现/引用
┌───────▼──────────────────────────────────▼──────────┐
│  MetaData.Application  （应用用例层）                │
│  Interfaces · Services · Models · QueryOperatorMap  │
└──────────────────────┬──────────────────────────────┘
                       │ 引用
┌──────────────────────▼──────────────────────────────┐
│  MetaData.Core  （领域层）                           │
│  Entities · Data(契约/映射) · Repositories(接口)     │
│  Abstractions/Services(IDbConnectionFactory 端口)   │
└───────┬──────────────────────────────┬──────────────┘
        │ 引用                         │ 引用
┌───────▼──────────────┐     ┌─────────▼──────────────┐
│ MetaData.Providers   │     │ MetaData.Abstractions  │
│ 方言/结构读取器/注册表│     │ 抽象契约（引用 Shared）│
└──────────────────────┘     └─────────┬──────────────┘
                                       │ 引用
                          ┌────────────▼──────────────┐
                          │ MyWebProject.Shared（基座）│
                          │ BaseEntity/仓储/UoW/分页/  │
                          │ BizException/Transactional│
                          └───────────────────────────┘
```

### 依赖方向

| 模块 | 引用 | 被引用 |
|---|---|---|
| `MyWebProject.Shared`（基座，跨解决方案） | 无（零依赖） | Abstractions、Core（传递）、Application、Infrastructure、Host |
| `MetaData.Abstractions` | Shared | Providers, Core |
| `MetaData.Providers` | Abstractions | Infrastructure |
| `MetaData.Core` | Abstractions + Shared + EFCore.Relational | Application, Infrastructure |
| `MetaData.Application` | Core + Abstractions + Shared（不引用 Providers/Infrastructure） | Web, Host |
| `MetaData.Infrastructure` | Core + Abstractions + Providers + AspNetCore.Framework（实现层） | Host |
| `MetaData.Web` | Application（类库，含 HTTP 接口） | Host |
| `MetaData.Host` | Web + Application + Infrastructure + Sqlite（SDK.Web，唯一启动点） | — |

> Application 面向 Core/Abstractions 的接口编程，并使用基座 `CurrentUserContext` 读取当前用户（不感知具体鉴权方式）；Infrastructure 只提供仓储/方言/数据保护实现；5 种方言/读取器与驱动的具体 `new` 装配只发生在 Infrastructure 与 Host。

## 3. 各模块职责

### 3.1 MyWebProject.Shared — 基座公共模块（跨解决方案引用）

> 位于 `../../MyWebTemplate/MyWebProject.Shared`，**零依赖**。本模块通过相对路径 `ProjectReference` 原地引用。

| 命名空间 | 内容 |
|---|---|
| `MyWebProject.Shared.Entities` | `BaseEntity`（`CreateAt`/`UpdateAt` 可空时间戳）、`BaseEntity<TKey>`（`TKey Id`） |
| `MyWebProject.Shared.Repositories` | `IQueryRepository<T>`（读模型）、`IRepository<T,TKey>`（增删改） |
| `MyWebProject.Shared.UnitOfWork` | `IUnitOfWork`（`SaveChangesAsync` + 事务三件套） |
| `MyWebProject.Shared.DTOs` | `PagedQuery`（PageIndex/PageSize/SkipCount/SortField/IsAscending）、`PagedResult<T>`（Items/TotalCount/PageIndex/PageSize/TotalPages）、`ApiResult` |
| `MyWebProject.Shared.Exceptions` | `BizException`（业务异常基类，默认 code=400） |
| `MyWebProject.Shared.Attributes` | `TransactionalAttribute`（基座 Autofac AOP 使用；本模块暂以显式 `IUnitOfWork.SaveChangesAsync` 保持等价） |

基座侧另有 `MyWebProject.Core.Entities.Common.AuditedEntity : BaseEntity<int>`（补 `CreateBy`/`UpdateBy` 审计人），仅基座实体使用；MetaData 实体不带审计人。

### 3.2 MetaData.Abstractions — 抽象契约层

> 依赖 Shared。定义跨模块共享的接口、枚举、异常与 Schema 契约。可独立打包为 NuGet 供其他项目引用。

| 命名空间 | 内容 |
|---|---|
| `MetaData.Abstractions` | `IDatabaseDialect`、`IDbProviderRegistry`、`IDialectRegistry`、`ISchemaInspectorRegistry`、`ICurrentUser`（UserId 为 string）、`ISecretProtector`、`DatabaseProviderRegistration`、`ConnectionSettings`、`DbVersionInfo`、`QueryParts`、`FilterCondition`、`SortItem`、`BuiltSql` |
| `MetaData.Abstractions.Enums` | `DatabaseType`、`ConnectionInputMode`、`AuthMode`、`OracleTargetType`、`DataCategory`、`FilterOperator`、`SortDirection` |
| `MetaData.Abstractions.Exceptions` | `MetaDataException : BizException`、`DriverNotRegisteredException : MetaDataException` |
| `MetaData.Abstractions.Schema` | `ISchemaInspector`、`ITableSchemaSample`、`IColumnSchemaSample` |

**为什么不含业务 DTO**：DTO 与具体业务场景绑定，属于应用层契约而非跨模块抽象。放在抽象层会让纯引用抽象的模块被迫依赖业务模型，破坏复用边界。

### 3.3 MetaData.Providers — 数据库方言实现层

> 依赖 Abstractions。包含 5 种数据库的方言与结构读取器，不引用任何具体 ADO.NET 驱动包（驱动由宿主注册）。

| 命名空间 | 内容 |
|---|---|
| `MetaData.Providers.Dialects` | `DatabaseDialectBase`（公共 WHERE/ORDER BY/分页/连接串）、5 方言实现（MySql/PostgreSql/SqlServer/Db2/Oracle） |
| `MetaData.Providers.Dialects.Building` | `SqlBuildContext`（internal，SQL 参数命名与收集） |
| `MetaData.Providers.Registries` | `DialectRegistry`、`DbProviderRegistry`、`SchemaInspectorRegistry` |
| `MetaData.Providers.SchemaInspection` | `SchemaInspectorBase`、5 读取器实现、`ReaderExtensions`、`TableSchemaSample`/`ColumnSchemaSample`（internal 默认实现） |

**分页版本分支**：MySQL/PostgreSQL 用 LIMIT OFFSET；SQL Server ≥2012、Oracle ≥12、DB2 ≥10.1 用 OFFSET FETCH；旧版本回退 ROW_NUMBER / ROWNUM 包装。

### 3.4 MetaData.Core — 领域层

> 依赖 Abstractions + Shared + EFCore.Relational（不引用 Providers，不引用 AspNetCore）。承载实体、模块 DbContext 契约、仓储接口、出站端口。不含业务服务、DTO、Controller、EF 配置。

| 命名空间 | 内容 |
|---|---|
| `MetaData.Core.Entities` | `DbConnectionInfo`、`TableMetadata`、`FieldMetadata`、`UserFieldPreference`，均继承 `BaseEntity<long>`（Id/CreateAt/UpdateAt 由基类提供，实体不重复声明） |
| `MetaData.Core.Data` | `IMetaDataDbContext`（4 个 DbSet + 泛型 `Set<TEntity>()` + `SaveChangesAsync` + `BeginTransactionAsync`）；模块不提供 DbContext，EF 配置在 Infrastructure 按 `IEntityTypeConfiguration<T>` 拆分 |
| `MetaData.Core.Repositories` | `IDbConnectionInfoRepository`、`ITableMetadataRepository`、`IFieldMetadataRepository`、`IUserFieldPreferenceRepository`（均继承 Shared `IRepository<T,long>`，只声明本模块专用查询） |
| `MetaData.Core.Abstractions.Services` | `IDbConnectionFactory`（出站端口：`OpenAsync(DbConnectionInfo, CancellationToken)`） |

### 3.5 MetaData.Application — 应用用例层

> 依赖 Core + Abstractions。业务编排与 DTO，面向接口与仓储，不直接使用 DbContext，不引用任何驱动/基础设施具体类。

| 命名空间 | 内容 |
|---|---|
| `MetaData.Application.Interfaces` | `IConnectionService`、`IMetadataService`、`IMetadataImportService`、`IDataQueryService`、`IUserPreferenceService` |
| `MetaData.Application.Services` | 上述 5 接口实现 + `QueryOperatorMap`（数据分类→允许操作符的纯逻辑映射，MetaController 复用） |
| `MetaData.Application/Models` | 24 个业务 DTO。`DataQueryRequest : PagedQuery`（pageIndex/pageSize），`DataQueryResponse : PagedResult<Dictionary<string,object?>>`（items/totalCount/pageIndex/pageSize/totalPages + columns） |
| `MetaData.Application` | `ApplicationServiceCollectionExtensions.AddMetaDataApplication`（5 服务按接口→实现 scoped 注册） |

> 用户身份：服务通过基座 `MyWebProject.Shared.CurrentUserContext`（`AsyncLocal<int?>`）读取当前用户，不依赖具体鉴权实现。未接入鉴权时回落 `"default"`。

**持久化模式**：服务注入仓储 + `IUnitOfWork`，查询走仓储专用方法（no-tracking/tracking 由方法名区分），写操作后显式 `unitOfWork.SaveChangesAsync()`；审计时间戳由宿主 AppDbContext 统一填充，服务不再手动赋值。

**ADO 查询边界不变**：`DataQueryService` 的全部 ADO.NET 逻辑（BuildCountQuery/BuildPagedQuery、CreateCommand 反射 BindByName、NormalizeValue、ConvertFilterValue 类型转换、别名映射、偏好合并、排序回退）原样保留。

### 3.6 MetaData.Infrastructure — 基础设施层

> 依赖 Core + Abstractions + Providers（`FrameworkReference Microsoft.AspNetCore.App`，DataProtection 需要）。实现仓储/工作单元/出站端口，装配方言、结构读取器与安全组件。

| 命名空间/文件 | 职责 |
|---|---|
| `MetaData.Infrastructure.Repositories` | `MetaDataRepositoryBase<T,TKey>`（面向 `IMetaDataDbContext` 实现 Shared 仓储契约，动态排序用 System.Linq.Dynamic.Core）+ 4 个 `internal sealed` 仓储实现 |
| `MetaData.Infrastructure.UnitOfWork` | `MetaDataUnitOfWork`（internal，转发 SaveChanges/托管事务） |
| `MetaData.Infrastructure.Data` | `DbConnectionFactory`（internal，解析驱动/方言→解密→拼串→打开连接） |
| `MetaData.Infrastructure.Security` | `DataProtectionSecretProtector`（internal，ASP.NET Core DataProtection） |
| `MetaData.Infrastructure.Data.Configurations` | 4 个 `IEntityTypeConfiguration<T>`（按实体拆分，宿主 OnModelCreating 中 `ApplyConfigurationsFromAssembly` 加载） |
| `InfrastructureServiceCollectionExtensions` | `AddMetaDataInfrastructure`：DataProtection、驱动注册表、5 方言+5 读取器、4 仓储、IUnitOfWork、IDbConnectionFactory；`AddDatabaseProvider(DatabaseType, DbProviderFactory)` 宿主注册驱动入口 |

### 3.7 MetaData.Web — HTTP 接口层

> 类库（`Microsoft.NET.Sdk` + `FrameworkReference Microsoft.AspNetCore.App`），依赖 Application。只承载 HTTP 入站适配器，构造函数注入 `IXxxService` 接口，不含启动代码与宿主配置。

| 命名空间/文件 | 职责 |
|---|---|
| `MetaData.Web.Controllers` | `ConnectionsController`、`MetadataController`、`DataController`、`MetaController`（仅 GET/POST，GET 仅用于无参请求） |
| `MetaDataWebServiceCollectionExtensions` | `AddMetaDataWeb`：`AddControllers().AddApplicationPart(本程序集)` + JSON 枚举字符串 |

### 3.8 MetaData.Host — 启动宿主

> `Microsoft.NET.Sdk.Web`，唯一启动点。仅做配置与模块装配：定义多模块共享的集成 DbContext、EF Core 提供程序、Swagger、CORS、静态文件、按需注册 ADO.NET 驱动、异常处理中间件。

| 文件 | 职责 |
|---|---|
| `Program.cs` | `AddDbContext<AppDbContext>(...)` + 接口映射、`AddMetaDataInfrastructure()` + `AddMetaDataApplication()` + `AddMetaDataWeb()`、`AddDatabaseProvider(...)`、`MetaDataException` 转 400 JSON、EnsureCreated、Swagger、静态文件 + SPA 回退 |
| `Data/AppDbContext.cs` | 宿主集成 DbContext，实现 `IMetaDataDbContext`（`Set<T>`/SaveChanges 复用基类成员，BeginTransaction 转发 Database）；override SaveChangesAsync 统一填充 `BaseEntity` 审计时间戳；OnModelCreating 通过 `ApplyConfigurationsFromAssembly` 加载模块的 `IEntityTypeConfiguration`；新增模块时在此追加其配置程序集 |
| `appsettings*.json` | 宿主配置 |
| `MetaData.Host.csproj` | 引用 Web + Application + Infrastructure + Sqlite + Swashbuckle |
| `wwwroot/` | Angular 构建产物 |

**多模块组合**：新增业务模块时，在本项目引用其 Web（HttpApi）+ Application + Infrastructure 项目并调用对应的注册扩展即可，无需改动既有模块。

## 4. 关键设计约束

### 4.1 驱动零耦合
`Abstractions`、`Core`、`Application`、`Web` 均不引用任何 ADO.NET 驱动 NuGet 包。宿主通过 `AddDatabaseProvider(DatabaseType.MySql, MySqlConnectorFactory.Instance)` 按需注册，未注册时返回中文 400 引导。

### 4.2 一类一文件
每个 `.cs` 文件只包含一个顶层类型，文件名即类型名（详见 `.trae/rules/csharp-one-type-per-file.md`）。

### 4.3 仅 GET/POST，参数统一 POST + JSON
HTTP 接口不使用 PUT/DELETE/PATCH，不使用路由参数。GET 仅用于无参查询（如 `list`、`operators`、`published-tables`）；凡需要传参的请求统一使用 POST + JSON body，不使用 query string（文件上传等 multipart 场景除外）。

### 4.4 暂不鉴权
当前用户统一使用基座 `MyWebProject.Shared.CurrentUserContext`（`AsyncLocal<int?>`），由宿主中间件按请求设置。未接入鉴权时 MetaData 服务回落 `"default"`。用户偏好按 UserId（string）隔离。

### 4.5 EF 管元数据 / ADO 查业务
元数据（连接/表/字段/偏好）用 EF Core 经仓储管理；业务库数据查询用 ADO.NET，由方言接口生成方言化 SQL（分页、参数前缀、引用符）。

### 4.6 异常与分页契约
- 模块业务异常 `MetaDataException` 继承基座 `BizException`，宿主中间件按 400 输出 `{ success, message }`。
- 分页请求/响应统一继承 Shared 的 `PagedQuery`/`PagedResult<T>`，字段名为 `pageIndex`/`pageSize`/`items`/`totalCount`/`totalPages`（前端已同步）。

## 5. 扩展指南

| 场景 | 改动位置 |
|---|---|
| 新增数据库支持 | `Providers/Dialects/` + `Providers/SchemaInspection/`，Infrastructure 装配，宿主注册驱动 |
| 新增业务接口 | `Web/Controllers/` + `Application/Interfaces` + `Application/Services` + `Application/Models` DTO |
| 新增数据访问 | `Core/Repositories` 声明接口方法 → `Infrastructure/Repositories` 实现，服务只依赖接口 |
| 新增元数据实体 | `Core/Entities/`（继承 `BaseEntity<long>`）+ `Infrastructure/Data/Configurations/` 新增 `IEntityTypeConfiguration` + `IMetaDataDbContext` 补 DbSet + 仓储 |
| 跨模块通用抽象 | 下沉到基座 `MyWebProject.Shared`（保持足够通用，零业务语义） |
| 接入鉴权 | 宿主（如基座 `CurrentUserMiddleware`）在请求开始时设置 `CurrentUserContext.UserId`；模块侧无需改动 |
| 替换元数据库 | 宿主 `Program.cs` 改 EF 提供程序（如 `UseNpgsql`） |
| 前端独立引用 DTO | 抽象层 Enums/Exceptions 可引用；业务 DTO 在 Application 中，按需引用 Application 或抽到共享契约项目 |

## 6. 目录结构

```
backend/
├── MetaData.slnx                # .NET 10 解决方案（新格式）
├── ARCHITECTURE.md              # 本文档
├── src/
│   ├── MetaData.Abstractions/   # 抽象契约层（引用 Shared）
│   │   ├── Enums/               # 7 枚举
│   │   ├── Exceptions/          # MetaDataException : BizException, DriverNotRegisteredException
│   │   ├── Schema/              # ISchemaInspector, ITableSchemaSample, IColumnSchemaSample
│   │   └── *.cs                 # IDatabaseDialect, IDbProviderRegistry, ...
│   ├── MetaData.Providers/      # 数据库方言实现
│   │   ├── Dialects/            # DatabaseDialectBase, 5 方言
│   │   │   └── Building/        # SqlBuildContext (internal)
│   │   ├── Registries/          # DialectRegistry, DbProviderRegistry, SchemaInspectorRegistry
│   │   └── SchemaInspection/    # SchemaInspectorBase, 5 读取器, 样本默认实现
│   ├── MetaData.Core/           # 领域层
│   │   ├── Abstractions/Services/ # IDbConnectionFactory 端口
│   │   ├── Data/                # IMetaDataDbContext 契约
│   │   ├── Entities/            # 4 EF 实体 : BaseEntity<long>
│   │   └── Repositories/        # 4 仓储接口
│   ├── MetaData.Application/    # 应用用例层
│   │   ├── Interfaces/          # 5 IXxxService
│   │   ├── Models/              # 24 业务 DTO（含 PagedQuery/PagedResult 分页契约）
│   │   ├── Services/            # 5 服务实现（仓储化）+ QueryOperatorMap
│   │   └── ApplicationServiceCollectionExtensions.cs  # AddMetaDataApplication
│   ├── MetaData.Infrastructure/ # 基础设施层
│   │   ├── Data/                # DbConnectionFactory (internal)
│   │   ├── Data/Configurations/ # 4 个 IEntityTypeConfiguration
│   │   ├── Repositories/        # MetaDataRepositoryBase + 4 仓储实现 (internal sealed)
│   │   ├── Security/            # DataProtectionSecretProtector (internal)
│   │   ├── UnitOfWork/          # MetaDataUnitOfWork (internal)
│   │   └── InfrastructureServiceCollectionExtensions.cs # AddMetaDataInfrastructure/AddDatabaseProvider
│   ├── MetaData.Web/            # HTTP 接口层（类库，面向 IXxxService）
│   │   ├── Controllers/         # 4 Controller
│   │   └── MetaDataWebServiceCollectionExtensions.cs
│   └── MetaData.Host/           # 启动宿主（唯一启动点）
│       ├── Data/                # AppDbContext（集成 DbContext + 审计填充 + 事务）
│       ├── Program.cs
│       ├── Properties/
│       ├── appsettings*.json
│       └── wwwroot/             # Angular 构建产物
└── tests/
    └── MetaData.Core.Tests/     # xUnit（46 测试，引用 Abstractions + Providers）
```
