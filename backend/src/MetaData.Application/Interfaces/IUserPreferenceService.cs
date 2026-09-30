using MetaData.Application.Models;

namespace MetaData.Application.Interfaces;

/// <summary>用户级字段偏好（顺序、显隐、宽度）。</summary>
public interface IUserPreferenceService
{
    Task<List<FieldPreferenceDto>> GetAsync(long tableId, CancellationToken cancellationToken = default);

    Task SaveAsync(SavePreferencesRequest request, CancellationToken cancellationToken = default);
}
