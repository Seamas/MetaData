namespace MetaData.Core.Abstractions;

/// <summary>数据库版本信息。</summary>
public class DbVersionInfo
{
    public static readonly DbVersionInfo Unknown = new() { Raw = string.Empty, Major = 0, Minor = 0 };

    public string Raw { get; set; } = string.Empty;

    public int Major { get; set; }

    public int? Minor { get; set; }

    /// <summary>版本是否不低于指定主.次版本（未知版本按 false 处理，由方言保守降级）。</summary>
    public bool AtLeast(int major, int minor = 0)
        => Major > major || (Major == major && (Minor ?? 0) >= minor);

    public override string ToString() => Raw;
}
