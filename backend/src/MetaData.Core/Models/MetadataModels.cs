using MetaData.Core.Enums;

namespace MetaData.Core.Models;

public class TableDto
{
    public long Id { get; set; }

    public long ConnectionId { get; set; }

    public string? Schema { get; set; }

    public string TableName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public bool IsPublished { get; set; }

    public string? DefaultSortField { get; set; }

    public string? Remark { get; set; }

    public int FieldCount { get; set; }
}

public class FieldDto
{
    public long Id { get; set; }

    public long TableId { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public string Alias { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public int Ordinal { get; set; }

    public bool IsVisible { get; set; } = true;

    public string? NativeDataType { get; set; }

    public DataCategory DataCategory { get; set; } = DataCategory.Unknown;

    public int? MaxLength { get; set; }

    public int? NumericPrecision { get; set; }

    public int? NumericScale { get; set; }

    public bool IsNullable { get; set; } = true;

    public bool IsPrimaryKey { get; set; }

    public string? Remark { get; set; }
}

/// <summary>业务库中的表（导入前选择）。</summary>
public class SourceTableDto
{
    public string? Schema { get; set; }

    public string TableName { get; set; } = string.Empty;

    public string? Comment { get; set; }
}

public class ImportRequestItem
{
    public string? Schema { get; set; }

    public string TableName { get; set; } = string.Empty;
}

public class MetadataImportRequest
{
    public long ConnectionId { get; set; }

    public List<ImportRequestItem> Tables { get; set; } = [];
}

public class MetadataImportResultDto
{
    public int AddedTables { get; set; }

    public int UpdatedTables { get; set; }

    public int AddedFields { get; set; }

    public int UpdatedFields { get; set; }

    /// <summary>业务库已不存在、但元数据中仍保留的字段（不自动删除）。</summary>
    public List<string> MissingFields { get; set; } = [];
}

public class SaveFieldsRequest
{
    public long TableId { get; set; }

    public List<FieldDto> Fields { get; set; } = [];
}

public class PublishTableRequest
{
    public long TableId { get; set; }

    public bool IsPublished { get; set; }
}

public class IdRequest
{
    public long Id { get; set; }
}
