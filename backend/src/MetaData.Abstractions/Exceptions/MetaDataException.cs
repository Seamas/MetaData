using MyWebProject.Shared.Exceptions;

namespace MetaData.Abstractions.Exceptions;

/// <summary>MetaData 模块异常：消息可直接展示给前端。继承公共 <see cref="BizException"/>，宿主的统一异常处理可直接识别。</summary>
public class MetaDataException(string message) : BizException(message);
