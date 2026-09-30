using MetaData.Application.Models;

namespace MetaData.Application.Interfaces;

/// <summary>从业务库读取结构并导入为元数据。</summary>
public interface IMetadataImportService
{
    Task<MetadataImportResultDto> ImportAsync(MetadataImportRequest request, CancellationToken cancellationToken = default);
}
