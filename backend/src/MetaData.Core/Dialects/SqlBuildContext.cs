namespace MetaData.Core.Dialects;

/// <summary>SQL 生成上下文：参数命名与收集。</summary>
public class SqlBuildContext
{
    private int _sequence;

    public SqlBuildContext(char parameterPrefix)
    {
        ParameterPrefix = parameterPrefix;
    }

    public char ParameterPrefix { get; }

    public Dictionary<string, object?> Parameters { get; } = new();

    /// <summary>登记一个自动命名的过滤参数，返回带前缀的占位符。</summary>
    public string Param(object? value)
    {
        var name = "f" + _sequence++;
        Parameters[name] = value;
        return ParameterPrefix + name;
    }

    /// <summary>登记保留命名参数（分页等），返回带前缀的占位符。</summary>
    public string Reserved(string name, object? value)
    {
        Parameters[name] = value;
        return ParameterPrefix + name;
    }
}
