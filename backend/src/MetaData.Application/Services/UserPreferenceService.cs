using MetaData.Abstractions;
using MetaData.Abstractions.Exceptions;
using MetaData.Application.Interfaces;
using MetaData.Application.Models;
using MetaData.Core.Entities;
using MetaData.Core.Repositories;
using Wang.Seamas.Shared;
using Wang.Seamas.Shared.UnitOfWork;

namespace MetaData.Application.Services;

/// <summary>用户级字段偏好（顺序、显隐、宽度）。</summary>
public class UserPreferenceService(
    IFieldMetadataRepository fields,
    IUserFieldPreferenceRepository preferences,
    IUnitOfWork unitOfWork) : IUserPreferenceService
{
    /// <summary>当前用户标识：基座 AsyncLocal 上下文，未接入鉴权时回落 default。</summary>
    private static string CurrentUserId => CurrentUserContext.UserId?.ToString() ?? "default";

    public async Task<List<FieldPreferenceDto>> GetAsync(long tableId, CancellationToken cancellationToken = default)
    {
        var list = await preferences.ListByUserTableAsync(CurrentUserId, tableId, cancellationToken);

        return list.Select(x => new FieldPreferenceDto
        {
            FieldId = x.FieldId,
            Ordinal = x.Ordinal,
            IsVisible = x.IsVisible,
            Width = x.Width
        }).ToList();
    }

    public async Task SaveAsync(SavePreferencesRequest request, CancellationToken cancellationToken = default)
    {
        var tableFields = await fields.ListByTableAsync(request.TableId, cancellationToken);
        if (tableFields.Count == 0)
        {
            throw new MetaDataException($"表不存在或没有字段：{request.TableId}");
        }

        var stored = await preferences.ListByUserTableForUpdateAsync(CurrentUserId, request.TableId, cancellationToken);

        foreach (var item in request.Items)
        {
            if (!tableFields.Exists(f => f.Id == item.FieldId))
            {
                throw new MetaDataException($"字段不属于该表：{item.FieldId}");
            }

            var entity = stored.FirstOrDefault(x => x.FieldId == item.FieldId);

            if (entity is null)
            {
                entity = new UserFieldPreference
                {
                    UserId = CurrentUserId,
                    TableId = request.TableId,
                    FieldId = item.FieldId,
                    Ordinal = item.Ordinal,
                    IsVisible = item.IsVisible,
                    Width = item.Width
                };
                stored.Add(entity);
                await preferences.AddAsync(entity, cancellationToken);
            }
            else
            {
                entity.Ordinal = item.Ordinal;
                entity.IsVisible = item.IsVisible;
                entity.Width = item.Width;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
