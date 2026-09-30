using System.Text.Json;
using MetaData.Abstractions.Exceptions;
using MetaData.Core;
using MetaData.Host.Data;
using MetaData.Web;
using Microsoft.EntityFrameworkCore;

// MetaData.Host：启动宿主，负责组合业务模块（MetaData.Core + MetaData.Web）。
// 模块化场景下可在此引用多个业务模块的 Core/Web 统一装配，宿主本身不含业务逻辑。
var builder = WebApplication.CreateBuilder(args);

// 1) 集成 DbContext：多模块共享，连接串与提供程序由宿主决定（示例 SQLite 零配置启动）。
//    如需 PostgreSQL/SQLServer/MySQL，改为 options.UseNpgsql(...) / UseSqlServer(...) / UseMySql(...)。
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=metadata.db"));
builder.Services.AddScoped<MetaData.Core.Data.IMetaDataDbContext>(sp =>
    sp.GetRequiredService<AppDbContext>());

// 2) MetaData 模块：业务服务 + HTTP 接口。
builder.Services.AddMetaDataCore();
builder.Services.AddMetaDataWeb();

// 3) 业务库 ADO.NET 驱动按需注册（核心模块不引用任何驱动包）。
//    使用哪种库，就在本项目 dotnet add package 对应包，并取消下面相应注释：
//
// using MetaData.Abstractions.Enums;
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

// 4) 开发环境 CORS（宿主职责）。
builder.Services.AddCors(options => options.AddPolicy("MetaDataDev", policy =>
    policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
          .AllowAnyHeader()
          .AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 友好异常：MetaDataException（含驱动未注册等）统一转 400 JSON。
// 宿主也可替换为自己的异常处理组件。
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (MetaDataException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { success = false, message = ex.Message }));
    }
});

// 元数据建库（首版使用 EnsureCreated）
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<AppDbContext>()
        .Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseCors("MetaDataDev");
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// Angular 构建产物放入 wwwroot，由宿主统一承载
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
