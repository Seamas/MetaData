---
description: C# 代码组织规则——每个 .cs 文件只能包含一个顶层类型，文件名与类型名一致
globs:
  - "**/*.cs"
alwaysApply: false
---

# C# 一类一文件规则

适用于本仓库（`backend/` 及后续所有 C# 项目）的所有 `.cs` 文件，包括生产代码与测试代码。

## 强制要求

1. **一个文件只定义一个顶层类型**：每个 `.cs` 文件只能包含一个顶层的 `class`、`record`、`struct`、`interface`、`delegate` 或 `enum`。
2. **文件名即类型名**：文件名为 `{类型名}.cs`，例如 `ConnectionDto.cs` 中只有 `ConnectionDto`；接口文件为 `I{名称}.cs`（如 `IDialectRegistry.cs`）。
3. **新建类型必须新建文件**：需要增加 DTO、异常、接口、枚举等类型时，创建独立文件，不得追加到"看起来相关"的现有文件（如 `*Models.cs`、`Common.cs`、`Enums.cs` 之类聚合文件禁止出现）。
4. **位置与命名空间一致**：文件放在与其命名空间分层对应的目录中（如命名空间 `MetaData.Core.Dialects` → `src/MetaData.Core/Dialects/`）；拆分时只移动文件，不改变命名空间，避免波及调用方。
5. **嵌套类型豁免**：声明在类型内部的私有/受保护嵌套类型（如类内部的 `protected enum`、私有辅助 struct）属于宿主类型的实现细节，保留在同一文件内，不算违规。
6. **聚合文件存量处理**：发现一个文件含多个顶层类型时，按类型逐一拆分为独立文件（保留 XML 注释、特性、using 指令），原文件删除；拆分后必须 `dotnet build` 与 `dotnet test` 验证。
7. **验证方法**：可用以下 PowerShell 扫描，结果必须为空：

   ```powershell
   Get-ChildItem -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch "\\obj\\|\\bin\\" } | ForEach-Object {
     $c = Get-Content $_.FullName -Raw
     $m = [regex]::Matches($c, '(?m)^(?:public|internal)\s+(?:abstract\s+|sealed\s+|static\s+|partial\s+)*(?:class|record|struct|interface|enum|delegate)\s+[\w<]')
     if ($m.Count -gt 1) { $_.FullName }
   }
   ```

## 反例与正例

- 反例：`DataQueryModels.cs` 中同时放 `FilterDto`、`SortDto`、`DataQueryRequest` 等 8 个类。
- 正例：`FilterDto.cs`、`SortDto.cs`、`DataQueryRequest.cs` 各自独立，同属命名空间 `MetaData.Core.Models`。
