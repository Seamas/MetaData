using MetaData.Abstractions;
using MetaData.Core.Data;
using MetaData.Abstractions.Exceptions;
using MetaData.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace MetaData.Core.Services;

/// <summary>用户级字段偏好（顺序、显隐、宽度）。</summary>
public class UserPreferenceService
{
    private readonly IDbContextFactory<MetaDataDbContext> _contextFactory;
    private readonly ICurrentUser _currentUser;

    public UserPreferenceService(IDbContextFactory<MetaDataDbContext> contextFactory, ICurrentUser currentUser)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
    }

    public async Task<List<FieldPreferenceDto>> GetAsync(long tableId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var list = await context.UserFieldPreferences.AsNoTracking()
            .Where(x => x.UserId == _currentUser.UserId && x.TableId == tableId)
            .OrderBy(x => x.Ordinal)
            .ToListAsync(cancellationToken);

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
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var validFieldIds = await context.Fields.AsNoTracking()
            .Where(f => f.TableId == request.TableId)
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);
        if (validFieldIds.Count == 0)
        {
            throw new MetaDataException($"表不存在或没有字段：{request.TableId}");
        }

        foreach (var item in request.Items)
        {
            if (!validFieldIds.Contains(item.FieldId))
            {
                throw new MetaDataException($"字段不属于该表：{item.FieldId}");
            }

            var entity = await context.UserFieldPreferences.FirstOrDefaultAsync(
                x => x.UserId == _currentUser.UserId && x.TableId == request.TableId && x.FieldId == item.FieldId,
                cancellationToken);

            if (entity is null)
            {
                context.UserFieldPreferences.Add(new Entities.UserFieldPreference
                {
                    UserId = _currentUser.UserId,
                    TableId = request.TableId,
                    FieldId = item.FieldId,
                    Ordinal = item.Ordinal,
                    IsVisible = item.IsVisible,
                    Width = item.Width,
                    UpdatedAt = DateTime.Now
                });
            }
            else
            {
                entity.Ordinal = item.Ordinal;
                entity.IsVisible = item.IsVisible;
                entity.Width = item.Width;
                entity.UpdatedAt = DateTime.Now;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
