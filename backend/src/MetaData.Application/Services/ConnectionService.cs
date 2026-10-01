using System.Data.Common;
using MetaData.Abstractions;
using MetaData.Abstractions.Enums;
using MetaData.Abstractions.Exceptions;
using MetaData.Abstractions.Schema;
using MetaData.Application.Interfaces;
using MetaData.Application.Models;
using MetaData.Core.Abstractions.Services;
using MetaData.Core.Entities;
using MetaData.Core.Repositories;
using Wang.Seamas.Shared.UnitOfWork;

namespace MetaData.Application.Services;

/// <summary>数据库连接元数据管理与连接测试。</summary>
public class ConnectionService(
    IDbConnectionInfoRepository connections,
    IDbProviderRegistry providers,
    ISchemaInspectorRegistry inspectors,
    ISecretProtector protector,
    IDbConnectionFactory connectionFactory,
    IUnitOfWork unitOfWork) : IConnectionService
{
    public async Task<List<ConnectionDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var list = await connections.ListAsync(null, cancellationToken);
        return list.OrderBy(x => x.Id).Select(ToDto).ToList();
    }

    public async Task<long> SaveAsync(ConnectionDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new MetaDataException("连接名称不能为空。");
        }

        var nameExists = await connections.AnyAsync(x => x.Name == dto.Name.Trim() && x.Id != dto.Id, cancellationToken);
        if (nameExists)
        {
            throw new MetaDataException($"连接名称“{dto.Name.Trim()}”已存在。");
        }

        var entity = dto.Id > 0
            ? await connections.GetByIdAsync(dto.Id, cancellationToken)
              ?? throw new MetaDataException($"连接不存在：{dto.Id}")
            : new DbConnectionInfo();

        entity.Name = dto.Name.Trim();
        entity.DatabaseType = dto.DatabaseType;
        entity.InputMode = dto.InputMode;
        entity.Host = dto.Host?.Trim();
        entity.Port = dto.Port;
        entity.DatabaseName = dto.DatabaseName?.Trim();
        entity.UserName = dto.UserName?.Trim();
        entity.AuthMode = dto.AuthMode;
        entity.InstanceName = dto.InstanceName?.Trim();
        entity.OracleTargetType = dto.OracleTargetType;
        entity.ExtraOptions = dto.ExtraOptions?.Trim();
        entity.DefaultSchema = dto.DefaultSchema?.Trim();
        entity.IsEnabled = dto.IsEnabled;
        entity.Remark = dto.Remark;

        if (dto.InputMode == ConnectionInputMode.Advanced)
        {
            if (!string.IsNullOrWhiteSpace(dto.AdvancedConnectionString))
            {
                entity.AdvancedConnectionStringProtected = protector.Protect(dto.AdvancedConnectionString.Trim());
            }
            else if (dto.Id == 0)
            {
                throw new MetaDataException("高级模式下必须填写连接串。");
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(dto.Password))
            {
                entity.PasswordProtected = protector.Protect(dto.Password);
            }
            else if (dto.Id == 0 && dto.AuthMode != AuthMode.Integrated)
            {
                throw new MetaDataException("请填写密码。");
            }
        }

        if (dto.Id == 0)
        {
            await connections.AddAsync(entity, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await connections.GetByIdAsync(id, cancellationToken)
                     ?? throw new MetaDataException($"连接不存在：{id}");
        await connections.RemoveAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>测试连接：已保存且密码留空时使用已存凭据；未保存时使用表单中的明文值。成功时回写版本缓存。</summary>
    public async Task<ConnectionTestResultDto> TestAsync(ConnectionDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await BuildExecutionEntityAsync(dto, cancellationToken);
        var inspector = inspectors.Resolve(entity.DatabaseType);

        try
        {
            await using var connection = await connectionFactory.OpenAsync(entity, cancellationToken);
            var version = await inspector.GetVersionRawAsync(connection, cancellationToken);

            // 已保存的连接：回写版本
            if (entity.Id > 0)
            {
                var saved = await connections.GetByIdAsync(entity.Id, cancellationToken);
                if (saved is not null)
                {
                    saved.ServerVersion = version;
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            return new ConnectionTestResultDto { Success = true, ServerVersion = version };
        }
        catch (Exception ex) when (ex is MetaDataException or DbException or InvalidOperationException)
        {
            return new ConnectionTestResultDto { Success = false, Message = ex.Message };
        }
    }

    /// <summary>读取业务库中的表清单。</summary>
    public async Task<List<SourceTableDto>> GetSourceTablesAsync(long connectionId, string? schema, CancellationToken cancellationToken = default)
    {
        var entity = await GetStoredEntityAsync(connectionId, cancellationToken);
        var inspector = inspectors.Resolve(entity.DatabaseType);

        await using var connection = await connectionFactory.OpenAsync(entity, cancellationToken);
        var tables = await inspector.GetTablesAsync(connection, string.IsNullOrWhiteSpace(schema) ? entity.DefaultSchema : schema, cancellationToken);
        return tables.Select(t => new SourceTableDto { Schema = t.Schema, TableName = t.TableName, Comment = t.Comment }).ToList();
    }

    public async Task<DbConnectionInfo> GetStoredEntityAsync(long connectionId, CancellationToken cancellationToken = default)
    {
        return await connections.GetByIdNoTrackingAsync(connectionId, cancellationToken)
               ?? throw new MetaDataException($"连接不存在：{connectionId}");
    }

    /// <summary>构造用于执行（测试/查询）的连接实体：合并已存凭据与表单新值。</summary>
    private async Task<DbConnectionInfo> BuildExecutionEntityAsync(ConnectionDto dto, CancellationToken cancellationToken)
    {
        DbConnectionInfo entity;

        if (dto.Id > 0)
        {
            entity = await connections.GetByIdNoTrackingAsync(dto.Id, cancellationToken)
                     ?? throw new MetaDataException($"连接不存在：{dto.Id}");

            // 表单可编辑字段覆盖
            entity.InputMode = dto.InputMode;
            entity.DatabaseType = dto.DatabaseType;
            entity.Host = dto.Host?.Trim();
            entity.Port = dto.Port;
            entity.DatabaseName = dto.DatabaseName?.Trim();
            entity.UserName = dto.UserName?.Trim();
            entity.AuthMode = dto.AuthMode;
            entity.InstanceName = dto.InstanceName?.Trim();
            entity.OracleTargetType = dto.OracleTargetType;
            entity.ExtraOptions = dto.ExtraOptions?.Trim();

            if (dto.InputMode == ConnectionInputMode.Advanced)
            {
                if (!string.IsNullOrWhiteSpace(dto.AdvancedConnectionString))
                {
                    entity.AdvancedConnectionStringProtected = protector.Protect(dto.AdvancedConnectionString.Trim());
                }
            }
            else if (!string.IsNullOrEmpty(dto.Password))
            {
                entity.PasswordProtected = protector.Protect(dto.Password);
            }
        }
        else
        {
            entity = new DbConnectionInfo
            {
                DatabaseType = dto.DatabaseType,
                InputMode = dto.InputMode,
                Host = dto.Host?.Trim(),
                Port = dto.Port,
                DatabaseName = dto.DatabaseName?.Trim(),
                UserName = dto.UserName?.Trim(),
                AuthMode = dto.AuthMode,
                InstanceName = dto.InstanceName?.Trim(),
                OracleTargetType = dto.OracleTargetType,
                ExtraOptions = dto.ExtraOptions?.Trim()
            };

            if (dto.InputMode == ConnectionInputMode.Advanced)
            {
                if (string.IsNullOrWhiteSpace(dto.AdvancedConnectionString))
                {
                    throw new MetaDataException("高级模式下必须填写连接串。");
                }

                entity.AdvancedConnectionStringProtected = protector.Protect(dto.AdvancedConnectionString.Trim());
            }
            else if (dto.AuthMode != AuthMode.Integrated)
            {
                if (string.IsNullOrEmpty(dto.Password))
                {
                    throw new MetaDataException("请填写密码。");
                }

                entity.PasswordProtected = protector.Protect(dto.Password);
            }
        }

        // 提前给出“驱动未注册”的友好错误
        if (!providers.IsRegistered(entity.DatabaseType))
        {
            providers.Resolve(entity.DatabaseType);
        }

        return entity;
    }

    public static ConnectionDto ToDto(DbConnectionInfo e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        DatabaseType = e.DatabaseType,
        InputMode = e.InputMode,
        Host = e.Host,
        Port = e.Port,
        DatabaseName = e.DatabaseName,
        UserName = e.UserName,
        Password = null,
        AuthMode = e.AuthMode,
        InstanceName = e.InstanceName,
        OracleTargetType = e.OracleTargetType,
        ExtraOptions = e.ExtraOptions,
        AdvancedConnectionString = null,
        ServerVersion = e.ServerVersion,
        DefaultSchema = e.DefaultSchema,
        IsEnabled = e.IsEnabled,
        Remark = e.Remark,
        HasPassword = !string.IsNullOrEmpty(e.PasswordProtected),
        HasAdvancedConnectionString = !string.IsNullOrEmpty(e.AdvancedConnectionStringProtected)
    };
}
