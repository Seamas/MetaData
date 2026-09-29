namespace MetaData.Core.Models;

/// <summary>元数据导入结果。</summary>
public class MetadataImportResultDto
{
    public int AddedTables { get; set; }

    public int UpdatedTables { get; set; }

    public int AddedFields { get; set; }

    public int UpdatedFields { get; set; }

    /// <summary>业务库已不存在、但元数据中仍保留的字段（不自动删除）。</summary>
    public List<string> MissingFields { get; set; } = [];
}
