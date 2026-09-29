using System.Data.Common;

namespace MetaData.Core.SchemaInspection;

/// <summary>DbDataReader 可空值读取扩展。</summary>
internal static class ReaderExtensions
{
    public static string? GetNullableString(this DbDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    public static int? GetNullableInt32(this DbDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
}
