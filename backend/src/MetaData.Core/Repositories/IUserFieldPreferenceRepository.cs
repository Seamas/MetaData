using MetaData.Core.Entities;
using MyWebProject.Shared.Repositories;

namespace MetaData.Core.Repositories;

/// <summary>用户字段偏好仓储。</summary>
public interface IUserFieldPreferenceRepository : IRepository<UserFieldPreference, long>
{
    /// <summary>读取某用户在某表上的全部偏好（Ordinal 排序），不跟踪。</summary>
    Task<List<UserFieldPreference>> ListByUserTableAsync(string userId, long tableId, CancellationToken cancellationToken = default);

    /// <summary>读取某用户在某表上的全部偏好（Ordinal 排序），跟踪（保存偏好路径）。</summary>
    Task<List<UserFieldPreference>> ListByUserTableForUpdateAsync(string userId, long tableId, CancellationToken cancellationToken = default);
}
