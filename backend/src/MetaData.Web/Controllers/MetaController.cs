using MetaData.Application.Models;
using MetaData.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MetaData.Web.Controllers;

/// <summary>前端渲染所需的常量（数据分类 → 操作符）。</summary>
[ApiController]
[Route("api/meta")]
public class MetaController : ControllerBase
{
    [HttpGet("operators")]
    public List<CategoryOperators> GetOperators() => QueryOperatorMap.BuildDescriptorList();
}
