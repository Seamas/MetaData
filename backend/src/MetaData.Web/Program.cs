using MetaData.Core;
using Microsoft.EntityFrameworkCore;

// MetaData.Web：宿主启动程序，只做配置，功能全部来自 MetaData.Core 模块。
var builder = WebApplication.CreateBuilder(args);

// 1) 元数据库：具体 EF Core 提供程序由宿主决定（示例使用 SQLite，零配置启动）。
//    如需 PostgreSQL/SQLServer/MySQL，改为 options.UseNpgsql(...) / UseSqlServer(...) / UseMySql(...) 即可。
builder.Services.AddMetaDataCore(options => options.UseSqlite("Data Source=metadata.db"));

// 2) 业务库 ADO.NET 驱动按需注册（核心模块不引用任何驱动包）。
//    使用哪种库，就在本项目 dotnet add package 对应包，并取消下面相应注释：
//
// using MetaData.Core.Enums;
// using MySqlConnector;
// builder.Services.AddDatabaseProvider(DatabaseType.MySql, MySqlConnectorFactory.Instance);
//
// using Npgsql;
// builder.Services.AddDatabaseProvider(DatabaseType.PostgreSql, NpgsqlFactory.Instance);
//
// using Microsoft.Data.SqlClient;
// builder.Services.AddDatabaseProvider(DatabaseType.SqlServer, SqlClientFactory.Instance);
//
// using IBM.Data.Db2;
// builder.Services.AddDatabaseProvider(DatabaseType.Db2, DB2Factory.Instance);
//
// using Oracle.ManagedDataAccess.Client;
// builder.Services.AddDatabaseProvider(DatabaseType.Oracle, OracleClientFactory.Instance);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 业务异常中间件 + EnsureCreated 元数据建库
await app.UseMetaDataCoreAsync();

if (app.Environment.IsDevelopment())
{
    app.UseCors(MetaDataServiceCollectionExtensions.DevCorsPolicyName);
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// 生产部署时可将 Angular 构建产物放入 wwwroot，由宿主统一承载
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
