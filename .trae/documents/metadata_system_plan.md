# 元数据驱动单表查询管理系统 实施计划

## 一、调研结论

### 环境现状
- 当前目录 `d:\WorkSpace\dotnet\MetaData` 为空。
- 已安装 SDK：.NET 8/9/10（采用 **.NET 10**，LTS 且与 EF Core 10 对齐）。
- Node v20.20.0、npm 10.8.2、全局 Angular CLI 21.1.2（满足 Angular 21 对 Node `^20.19 || ^22.12` 的要求）。
- 前端组件库确认：`ng-zorro-antd@21.3.3` 的 peerDependencies 为 `@angular/* ^21`，匹配 Angular 21。

### 可用驱动版本（宿主按需引用，核心模块不引用）
| 数据库 | ADO.NET 驱动 NuGet 包 | 流行支持版本 |
|---|---|---|
| MySQL | MySqlConnector 2.6.x | MySQL 5.7 / 8.0 |
| PostgreSQL | Npgsql 10.x | PostgreSQL 12+ |
| SQL Server | Microsoft.Data.SqlClient 7.x | SQL Server 2012+ |
| DB2 | Net.IBM.Data.Db2 10.x（IBM 官方托管驱动，Windows/Linux 均可用） | DB2 for LUW 10.1+（10.5/11/12） |
| Oracle | Oracle.ManagedDataAccess 23.x（23 起统一包，无需 .Core 后缀） | Oracle 11gR2+（12c/19c/21c/23c） |
| 元数据库（宿主示例） | Microsoft.EntityFrameworkCore.Sqlite 10.x | SQLite 3 |

### 已确认的关键决策
1. **驱动解耦**：核心模块不引用任何具体数据库驱动。
   - EF Core：核心只依赖 `Microsoft.EntityFrameworkCore` 抽象，由宿主启动项目传入 `UseSqlite(...)`/`UseNpgsql(...)` 等配置委托。
   - ADO.NET：宿主通过注册扩展把各库的 `DbProviderFactory` 实例登记到注册表；未注册时给出明确友好错误。
2. **暂不做鉴权**：用 `ICurrentUser` 抽象表示用户身份，默认实现返回固定开发用户（配置可改），将来接入权限体系时替换实现即可；字段顺序/显隐偏好按 `UserId` 隔离。
3. 接口约束（遵循既有偏好）：**只用 GET / POST**，不使用 PUT/DELETE/PATCH，不使用路由参数（id 走 query string 或请求体）。

## 二、目录结构（前后端分离）

```
MetaData/
├── backend/
│   ├── MetaData.sln
│   ├── src/
│   │   ├── MetaData.Core/                 # 核心模块（类库，net10.0）
│   │   │   ├── Entities/                  # EF Core 实体
│   │   │   ├── Enums/                     # DatabaseType、DataCategory、FilterOperator、SortDirection
│   │   │   ├── Data/
│   │   │   │   ├── MetaDataDbContext.cs
│   │   │   │   └── DbSeeder.cs            # 初始数据（默认用户、示例开关等）
│   │   │   ├── Abstractions/              # 接口：方言、连接工厂、Schema 读取、当前用户、服务
│   │   │   ├── Dialects/                  # 5 种方言实现 + 版本能力判定
│   │   │   ├── SchemaInspection/          # 5 种库的结构读取 SQL 实现
│   │   │   ├── Services/                  # 元数据管理、结构导入、业务数据查询、用户偏好
│   │   │   ├── Models/                    # 请求/响应 DTO
│   │   │   ├── Security/                  # 连接串加密（IDataProtectionProvider）
│   │   │   ├── Api/                       # Controller（程序集自动注册为 ApplicationPart）
│   │   │   └── MetaDataServiceCollectionExtensions.cs
│   │   └── MetaData.Web/                  # 启动程序（ASP.NET Core，net10.0）
│   │       ├── Program.cs                 # 仅做配置：AddMetaData(o=>o.UseSqlite(...))、注册业务库驱动、Swagger、静态文件
│   │       ├── appsettings.json
│   │       └── wwwroot/                   # （发布时）承载 Angular 构建产物
│   └── tests/
│       └── MetaData.Core.Tests/           # xUnit：方言 SQL 生成单测（无需真实数据库）
└── frontend/
    └── metadata-web/                      # Angular 21 + ng-zorro 21 工程
        ├── proxy.conf.json                # /api → http://localhost:5080
        └── src/app/
            ├── core/                      # API 服务、DTO、拦截器
            ├── layout/                    # nz-layout 菜单框架
            └── pages/
                ├── connection/            # 数据库连接管理（含测试连接、读取表清单）
                ├── metadata/              # 表/字段元数据查看、编辑、导入
                └── data-query/            # 业务数据查询（动态表格、条件、分页、列个性化）
```

## 三、元数据模型设计（含补充字段）

### 1. DbConnectionInfo（数据库连接）
采用「**结构化字段为主 + 高级原始串兜底**」，连接串不落库，由方言按字段实时拼出：
- 基础：`Id`、`Name`（连接名称）、`DatabaseType`（枚举：MySql/PostgreSql/SqlServer/Db2/Oracle）、`IsEnabled`、`Remark`、`CreatedAt`、`UpdatedAt`
- 连接模式：`InputMode`（Simple 结构化 / Advanced 原始串）
- Simple 模式字段：`Host`、`Port`、`DatabaseName`（MySQL 库名 / PG 数据库 / SQLServer 库 / DB2 数据库 / **Oracle 的服务名或 SID**）、`UserName`、`PasswordProtected`（仅密码密文）、`AuthMode`（Basic 账号密码 / Integrated 集成认证，主要用于 SQLServer Windows 认证，此模式下用户密码留空）、`InstanceName`（SQLServer 可选实例名）、`OracleTargetType`（仅 Oracle：ServiceName / Sid）、`ExtraOptions`（高级附加参数原文，如 SSL、超时、字符集，按各驱动 key=value 语法追加）
- Advanced 模式字段：`AdvancedConnectionStringProtected`（整串密文），用于结构化表达不了的特殊场景
- `ServerVersion`（连接成功后探测并缓存）、`DefaultSchema`（可空，如 PG 的 public、Oracle 的用户名、SQLServer 的 dbo）
- 补充说明：各库连接要素与关键字差异（Oracle 的 Service Name/SID、SQLServer 实例名与集成认证、`Uid/Pwd` 与 `User ID/Password` 等不同关键字）统一由 `IDatabaseDialect.BuildConnectionString(...)` 屏蔽，前端按数据库类型动态渲染对应表单项。

### 2. TableMetadata（表信息）
`Id`、`ConnectionId`、`Schema`、`TableName`（物理表名）、`DisplayName`（中文名）、`IsPublished`（**管理员是否开放**给业务查询）、`DefaultSortField`、`Remark`、`CreatedAt`、`UpdatedAt`；唯一索引 `(ConnectionId, Schema, TableName)`。

### 3. FieldMetadata（字段信息）
`Id`、`TableId`、`FieldName`（物理字段名）、`Alias`（给前端的别名，表内唯一；默认等于字段名，可改为安全别名）、`DisplayName`（显示名/中文名）、`Ordinal`（顺序）、`IsVisible`（是否显示），补充：
- `NativeDataType`（原始类型字符串，如 `varchar(100)`、`NUMBER(10,2)`）
- `DataCategory`（归一化分类：String / Number / DateTime / Boolean / Guid / Binary / Unknown，决定前端条件操作符与控件）
- `MaxLength`、`NumericPrecision`、`NumericScale`、`IsNullable`、`IsPrimaryKey`、`Remark`；唯一索引 `(TableId, FieldName)`、`(TableId, Alias)`。

### 4. UserFieldPreference（用户列个性化）
`Id`、`UserId`、`TableId`、`FieldId`、`Ordinal`、`IsVisible`、`Width`（可空）；唯一索引 `(UserId, TableId, FieldId)`。
- 未配置的字段回退到 FieldMetadata 的默认顺序与显隐。

## 四、后端核心设计

### 1. 方言接口（Dialects/）

```csharp
public interface IDatabaseDialect
{
    DatabaseType DatabaseType { get; }
    char ParameterPrefix { get; }            // SQLServer/MySQL @,PG @,Oracle :,DB2 @
    string QuoteIdentifier(string name);     // MySQL `x`，PG/SQLServer/DB2 "x"，Oracle "X"
    string BuildPagedQuery(QueryParts parts, DbVersionInfo version); // 按版本生成分页 SQL
    string BuildCountQuery(QueryParts parts);
    string GetVersionSql();                  // 各库版本探测 SQL
    DbVersionInfo ParseVersion(string raw);
    DialectCapabilities GetCapabilities(DbVersionInfo version); // OffsetFetch 等特性开关
    string BuildConnectionString(DbConnectionInfo info);       // 由结构化字段拼各驱动专有连接串（Advanced 模式解密直返）；Oracle 区分 ServiceName/SID，SQLServer 处理实例名与集成认证
}
```

- `QueryParts`：Schema/表名、列名映射、WHERE 条件树、排序、分页参数；**全部参数化**，值以 `DbParameter` 绑定，杜绝拼接注入与隐式类型转换。
- 分页策略（按版本分支）：
  - MySQL 5.7+/8.0：`LIMIT @take OFFSET @skip`
  - PostgreSQL 12+：`LIMIT @take OFFSET @skip`
  - SQL Server 2012+：`OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY`；2008 及更早回退 `ROW_NUMBER() OVER(...)` 子查询
  - Oracle 12c+：`OFFSET :skip ROWS FETCH NEXT :take ROWS ONLY`；11g 回退 `WHERE rn > :skip AND rn <= :end` 的 ROWNUM 包装
  - DB2 10.1+：`OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY`；更早回退 `ROW_NUMBER() OVER(...)`
- 标识符大小写策略：MySQL 保留原名反引号；PG 按实际名加双引号；Oracle 未加引号默认大写，按目录返回的实际值原样引用；SQL Server 方括号/双引号；DB2 双引号大写。
- 各方言连接串拼法示例（Simple 模式，密码解密后填入）：
  - MySQL（MySqlConnector）：`Server={host};Port={port};Database={db};Uid={user};Pwd={pwd};{extras}`
  - PostgreSQL（Npgsql）：`Host={host};Port={port};Database={db};Username={user};Password={pwd};{extras}`
  - SQLServer（SqlClient）：`Server={host}[\{instance}][,{port}];Database={db};User Id={user};Password={pwd};`，集成认证时改 `Integrated Security=True;` 并忽略账号密码
  - DB2（Net.IBM.Data.Db2）：`Server={host}:{port};Database={db};UID={user};PWD={pwd};{extras}`
  - Oracle（ODP.NET）：ServiceName 用 `Data Source={host}:{port}/{service};User Id={user};Password={pwd};`；SID 用描述符 `Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port}))(CONNECT_DATA=(SID={sid})));...`

### 2. 驱动注册（Abstractions/）

```csharp
public interface IDbProviderRegistry
{
    void Register(DatabaseType type, DbProviderFactory factory);
    DbProviderFactory Resolve(DatabaseType type);   // 未注册抛友好异常："未注册 MySQL 驱动，请在启动项目引用 MySqlConnector 并调用 AddDatabaseProvider(...)"
    bool IsRegistered(DatabaseType type);
}
```
- 宿主用法（Program.cs 中以注释/示例给出）：
  `builder.Services.AddMetaData(o => o.UseSqlite("Data Source=metadata.db"));`
  `builder.Services.AddDatabaseProvider(DatabaseType.MySql, MySqlConnectorFactory.Instance);`
- `IDbConnectionFactory` 依据 `DbConnectionInfo.DatabaseType` 取 factory 并创建连接、设置连接串。

### 3. Schema 结构读取（SchemaInspection/）
`ISchemaInspector`：`GetVersionAsync`、`GetTablesAsync(schema)`、`GetColumnsAsync(schema, tableNames)`，全部经注册的 ADO 工厂执行：
- MySQL：`information_schema.TABLES/COLUMNS`，`TABLE_SCHEMA = DATABASE()`；PK 取 `COLUMN_KEY='PRI'`。
- PostgreSQL：`information_schema.tables/columns` + `pg_catalog` 查主键；schema 过滤。
- SQL Server：`INFORMATION_SCHEMA.TABLES/COLUMNS/KEY_COLUMN_USAGE`（2012+ 通用）。
- Oracle：`ALL_TABLES/ALL_TAB_COLUMNS/ALL_COL_COMMENTS/ALL_CONSTRAINTS+ALL_CONS_COLUMNS`；11g 起可用；类型取 `DATA_TYPE/DATA_LENGTH/DATA_PRECISION/DATA_SCALE`。
- DB2：`SYSCAT.TABLES/COLUMNS`（`KEYSEQ` 判主键，`TYPENAME/LENGTH/SCALE/NULLS` 取类型）。
- 统一映射到 `TableSchemaSample/ColumnSchemaSample`，再由 `IMetadataImportService` 合并写入：
  - 表按 `(Schema,TableName)` 匹配，存在则更新，不存在则新增；
  - 字段按 `FieldName` 匹配，**已存在字段保留用户配置的 Alias/DisplayName/Ordinal/IsVisible**，新字段追加（Ordinal 取当前最大值之后），返回新增/更新/缺失清单，不物理删除用户已配字段。
- 连接建立后立即执行版本探测并缓存 `ServerVersion`，方言按版本选择 SQL 模板。
- 各库原始类型 → `DataCategory` 映射表集中在 Dialect 内维护（如 Oracle NUMBER 无精度有规模按小数、有精度按整数等）。

### 4. 业务数据查询（Services/DataQueryService.cs，ADO.NET）
- 请求：`POST /api/data/query`，体：`{ tableId, page, pageSize(≤500), filters:[{alias, operator, value, value2}], sorts:[{alias, direction}] }`。
- 处理：按 tableId 取表+字段元数据并校验连接已启用、表已发布；**别名→真实字段名映射**；按 `DataCategory` 校验操作符与值（不变文化解析数字、ISO 日期）；通过方言构造参数化查询 + Count；经 `IDbConnectionFactory` 执行。
- 操作符集合（由后端常量接口下发给前端）：
  - String：等于/不等于/包含/开头是/结尾是/为空/非空
  - Number、DateTime：=、≠、>、≥、<、≤、区间、为空/非空
  - Boolean：等于；Guid：等于/为空；Binary：仅不支持查询提示
- 响应：`{ columns:[{alias, displayName, dataCategory, width?}], rows:[{别名: 值}], total, page, pageSize }`；`DBNull→null`，DateTime 输出 ISO，decimal 保留数值输出。
- 查询只选择可见列（管理员默认配置经用户偏好覆盖后的并集）。

### 5. 用户偏好、安全与模块化
- `ICurrentUser`：默认实现读配置 `MetaData:DefaultUserId`（默认 `default`），预留替换点。
- 密码/高级连接串安全：Simple 模式仅对 `PasswordProtected` 加密，Advanced 模式对整串加密，均使用 `IDataProtectionProvider`（密文前缀 `protected:`）；连接时由方言解密拼串，列表接口永不回显密码明文（回传掩码占位符）；无法解密时给出明确提示。
- 核心模块通过 `services.AddMetaDataCore(Action<DbContextOptionsBuilder> configure)` 注册：DbContext、方言注册表、各服务、`AddControllers().AddApplicationPart(MetaDataCore 程序集)`；宿主 Program.cs 仅几行配置即可启动（Swagger、JSON 日期、CORS 开发策略、`EnsureCreated` 建库 + Seeder）。
- 建库策略：首版用 `EnsureCreated()`（避免多提供程序下迁移复杂化），实体保持提供程序中立类型（string/int/long/DateTime/bool）。

### 6. HTTP 接口（仅 GET/POST，无路由参数）
- 连接：`GET /api/connections/list`、`POST /api/connections/save`、`POST /api/connections/test`、`POST /api/connections/delete`
- 结构导入：`GET /api/metadata/source-tables?connectionId=&schema=`、`POST /api/metadata/import`
- 元数据：`GET /api/metadata/tables?connectionId=`、`GET /api/metadata/fields?tableId=`、`POST /api/metadata/save-table`、`POST /api/metadata/save-fields`（批量）、`POST /api/metadata/publish`
- 查询：`GET /api/data/published-tables`、`POST /api/data/query`、`GET /api/data/preferences?tableId=`、`POST /api/data/preferences/save`
- 常量：`GET /api/meta/operators`（分类→操作符）

## 五、前端设计（Angular 21 + ng-zorro 21）

- `ng new metadata-web`（standalone、SCSS）后 `ng add ng-zorro-antd@21`；`proxy.conf.json` 代理 `/api`。
- 布局：nz-layout + nz-menu 三模块：**数据库连接 / 元数据管理 / 数据查询**。
- 数据库连接页：nz-table 列表（名称、类型徽标、版本、启用开关）；新增/编辑 nz-modal：Simple/Advanced 两种模式切换——Simple 模式按所选数据库类型动态渲染（主机、端口、库名/服务名，Oracle 额外出现 ServiceName/SID 选择，SQLServer 出现实例名与集成认证开关，集成认证时隐藏账号密码），密码框掩码且回显为占位符、留空表示不修改；Advanced 模式直接贴原始连接串；支持附加参数；**测试连接**；进入"读取表结构"抽屉勾选表后**导入元数据**，展示新增/更新/缺失结果。
- 元数据管理页：左连接+表 nz-tree，右侧表属性（中文名、发布开关）+ 字段 nz-table：行内编辑显示名/别名（表内唯一校验）、类型（只读+分类可修正）、是否显示、主键标识；顺序用拖拽（@angular/cdk drag-drop）调整批量保存。
- 数据查询页：选择已开放表 → 动态 nz-table：
  - 列按用户偏好合并默认配置渲染；列设置抽屉支持拖拽排序、显隐切换，保存到用户偏好；
  - 筛选行按 `dataCategory` 渲染：字符串 nz-select 操作符（包含等）+输入框；数字/日期 操作符+值（日期用 nz-date-picker，区间用 RangePicker）；布尔下拉；
  - nz-pagination 分页、点击表头排序、查询条件重置。
- core 层：强类型 DTO（与后端契约一致）、`HttpApiService`（统一只用 get/post）、错误用 nz-message 通知。

## 六、实施步骤（按依赖顺序）

1. 创建解决方案与项目骨架：`MetaData.Core`（类库+FrameworkReference Microsoft.AspNetCore.App）、`MetaData.Web`、`MetaData.Core.Tests`；Angular 工程并接入 ng-zorro 21、代理配置。
2. 后端实体、枚举、DbContext、Seeder、`AddMetaDataCore` 扩展；宿主 Program.cs 配置 SQLite（仅宿主引用 EFCore.Sqlite）。
3. 抽象层：`IDbProviderRegistry`/`IDbConnectionFactory`/`ICurrentUser`/`IDatabaseDialect`/`ISchemaInspector` + 注册表与 DI 注册扩展。
4. 5 种方言实现：标识符引用、参数前缀、版本解析、分页/Count SQL 生成、特性分支、类型映射表、`BuildConnectionString`（含 Oracle ServiceName/SID、SQLServer 实例名/集成认证）。
5. 5 种 Schema 读取实现与元数据导入合并服务；连接测试服务；密码/高级连接串加解密。
6. 元数据管理服务与 Controller（连接/表/字段/发布/导入/常量）。
7. 业务数据查询服务与 Controller（别名映射、条件校验、参数化、分页）、用户偏好接口。
8. xUnit 单测覆盖：5 方言分页 SQL（含 Oracle 11g/12c、DB2 10.1 前后、SQLServer 2012 前后分支）、标识符引用、连接串拼装（Oracle 两种目标、SQLServer 集成认证/实例名）、条件→SQL/参数生成、类型映射、别名校验。
9. 前端 layout 与 core API 层；连接管理页；元数据管理页（含导入）；数据查询页（筛选/分页/排序/列个性化）。
10. 启动联调：后端 EnsureCreated 建库、Swagger 验证；前端代理联调；`dotnet build` + `dotnet test` + `ng build` 全绿；修正问题。

## 七、依赖与注意事项

- MetaData.Core 仅加 `Microsoft.EntityFrameworkCore` 10 的抽象包与 `Microsoft.AspNetCore.App` 框架引用，**不引用** Sqlite/Npgsql/MySqlConnector/SqlClient/IBM/Oracle 任何驱动；驱动注册只在宿主发生。
- Web 宿主示例引用 `Microsoft.EntityFrameworkCore.Sqlite` 即可完整启动；业务库驱动包与注册代码在 Program.cs 中以注释示例提供。
- Oracle 11g 别名/标识符 30 字符限制（12.2 前）：UI 上对别名长度按连接版本给出提示。
- 大小写：按各库系统视图返回的真实名称存储与引用，不做臆测大小写转换。
- Angular 21 要求 Node ^20.19 || ^22.12，本机 20.20 满足；锁定 ng-zorro 21.x，避免升到 22。
- 遵守接口约束：全程 GET/POST、无路由参数、无 PUT/DELETE/PATCH。

## 八、验证方式

1. `dotnet build backend/MetaData.sln` 通过；`dotnet test` 方言单测全绿。
2. 启动 MetaData.Web：自动生成 SQLite 元数据库，Swagger 可浏览全部接口；未注册某业务库驱动时返回明确的友好错误。
3. `ng build` 生产构建通过；开发模式下经代理完成：连接新增（驱动由宿主注册后可测试连接/导入）、元数据编辑、业务表查询/条件/分页/列个性化保存与刷新回显。
4. 无真实数据库环境的方言（DB2/Oracle 等）通过单测验证生成的 SQL 文本与参数正确性；待接入真实库时再做集成验证。

## 九、风险与应对

- **无 5 种真实库环境**：方言/Schema SQL 以单测固化预期；结构读取相关 SQL 对照官方系统视图文档编写，接入真实库后仅需微调。
- **DB2 驱动平台差异**：采用 IBM 官方托管包 `Net.IBM.Data.Db2`（Win/Linux 可用），宿主按需引用，核心不耦合。
- **驱动加载失败/版本不匹配**：注册表层统一捕获，返回"驱动未注册/加载失败"中文提示，不影响元数据管理功能。
- **大 decimal/日期格式精度**：decimal 按数值序列化并在文档注明；日期统一 ISO 8601，绑定参数时显式类型，避免 ORA-01861 类隐式转换问题。
- **后续接入鉴权**：`ICurrentUser` 已隔离，替换实现 + 加鉴权中间件即可，不影响业务代码。
