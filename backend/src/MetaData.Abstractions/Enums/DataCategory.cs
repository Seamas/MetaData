namespace MetaData.Abstractions.Enums;

/// <summary>字段数据归一化分类，决定前端控件与查询操作符。</summary>
public enum DataCategory
{
    Unknown = 0,
    String = 1,
    Number = 2,
    DateTime = 3,
    Boolean = 4,
    Guid = 5,
    Binary = 6
}
