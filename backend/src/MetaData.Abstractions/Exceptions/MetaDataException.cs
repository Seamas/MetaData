namespace MetaData.Abstractions.Exceptions;

/// <summary>MetaData 模块异常：消息可直接展示给前端。</summary>
public class MetaDataException(string message) : Exception(message);
