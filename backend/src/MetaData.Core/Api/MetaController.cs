using MetaData.Core.Models;
using MetaData.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace MetaData.Core.Api;

/// <summary>前端渲染所需的常量（数据分类 → 操作符）。</summary>
[ApiController]
[Route("api/meta")]
public class MetaController : ControllerBase
{
    [HttpGet("operators")]
    public List<CategoryOperators> GetOperators() => QueryOperatorMap.BuildDescriptorList();
}
