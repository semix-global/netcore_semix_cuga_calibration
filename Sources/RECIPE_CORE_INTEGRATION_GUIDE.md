# CalibrationRecipe.Core 集成指南

## 快速开始

### 1. 确保项目引用正确

**Cuga-Calibration-Solution.csproj** 已包含:
```xml
<ProjectReference Include="..\CalibrationRecipe.Core\CalibrationRecipe.Core.csproj" />
```

### 2. 验证编译

```bash
cd E:\CalibrationHQ\netcore_semix_cuga_calibration\Sources

# 编译新项目
dotnet build CalibrationRecipe.Core/CalibrationRecipe.Core.csproj -c Debug

# 编译主项目
dotnet build Cuga-Calibration-Solution/Cuga-Calibration-Solution.csproj -c Debug
```

### 3. 运行测试

```bash
# 如果有单元测试项目
dotnet test Cuga-Calibration-Unit-Test/Cuga-Calibration-Unit-Test.csproj
```

---

## 常见问题

### Q: 编译报错找不到 CalibrationRecipe.Core

**A**: 确保:
1. `CalibrationRecipe.Core` 项目文件位置正确: `Sources/CalibrationRecipe.Core/CalibrationRecipe.Core.csproj`
2. `Cuga-Calibration-Solution.csproj` 中有正确的项目引用
3. 清理并重建解决方案: `dotnet clean && dotnet build`

### Q: 编译报错找不到某个命名空间

**A**: 检查是否遗漏了 using 语句更新。搜索旧的命名空间:
```bash
grep -r "using Core.Models.Models.Common.Recipe" --include="*.cs"
grep -r "using CugaCalibration.Core.Services.*Recipe" --include="*.cs"
```

如果找到，替换为对应的新命名空间。

### Q: 想要从其他项目引用配方模块

**A**: 只需在其他项目的 `.csproj` 文件中添加:
```xml
<ProjectReference Include="..\CalibrationRecipe.Core\CalibrationRecipe.Core.csproj" />
```

然后使用新的命名空间即可:
```csharp
using CalibrationRecipe.Core.Models;
using CalibrationRecipe.Core.Services;
```

---

## 模块接口速查

### 配方管理

```csharp
using CalibrationRecipe.Core.Services;

var managementService = serviceProvider.GetRequiredService<ICalibrationRecipeManagementService>();

// 创建
await managementService.CreateAsync(name, cancellationToken);

// 删除
await managementService.DeleteAsync(recipeDto, cancellationToken);

// 重命名
await managementService.RenameAsync(recipeDto, newName, cancellationToken);

// 导入
await managementService.ImportAsync(sourceFolder);

// 继承
var inheritedRecipe = await managementService.InheritAsync(sourceRecipe, cancellationToken);

// 应用
await managementService.ApplyAsync(recipeDto);
```

### 配方编辑

```csharp
using CalibrationRecipe.Core.Services;

var editorService = serviceProvider.GetRequiredService<ICalibrationRecipeEditorService>();

// 加载可编辑副本
var workingCopy = await editorService.LoadForEditAsync(readonlyRecipe);

// 重命名
var updated = await editorService.RenameRecipeAsync(readonlyRecipe, newName);

// 保存
await editorService.SaveRecipeAsync(workingCopy);
```

### 受保护的缓存访问

```csharp
using CalibrationRecipe.Core.Services;

var guardedCache = serviceProvider.GetRequiredService<IGuardedRecipeCacheProvider>();

// 加载只读配方（自动验证一致性）
var readonlyRecipe = await guardedCache.LoadCalibrationRecipeAsReadonlyAsync(recipeInfo);

// 保存配方（自动验证和同步）
await guardedCache.SaveCalibrationRecipeAsync(recipe);
```

### 数据一致性验证

```csharp
using CalibrationRecipe.Core.Services.Guardian;

var validator = serviceProvider.GetRequiredService<IRecipeDataConsistencyValidator>();

// 验证配方存在性
var result = await validator.ValidateRecipeExistenceAsync(recipeDto);

// 验证配方完整性
var result = await validator.ValidateRecipeIntegrityAsync(recipeDto);

// 验证配方不存在（用于创建前检查）
var result = await validator.ValidateRecipeNotExistsAsync(recipeDto);
```

---

## 模型类

```csharp
using CalibrationRecipe.Core.Models;

// 可编辑的配方 DTO
var recipe = new CalibrationRecipeDto 
{
    CalibrationRecipeInfoDto = new CalibrationRecipeInfoDto { /* ... */ },
    WaferDto = new WaferDto { /* ... */ }
};

// 只读包装（防止修改关键字段）
ICalibrationRecipeReadonly readonlyRecipe = new CalibrationRecipeReadonlyWrapper(recipe, sysRecipeInfo);

var id = readonlyRecipe.Id;  // ✅ 可读
var name = readonlyRecipe.RecipeName;  // ✅ 可读
// readonlyRecipe.Id = 999;  // ❌ 编译错误
```

---

## 项目依赖树

```
Cuga-Calibration-Solution
    └─> CalibrationRecipe.Core
        ├─> CommunityToolkit.Mvvm
        ├─> Microsoft.Extensions.*
        ├─> Local.SQL.Cache.Providers
        ├─> Local.SQL.DB.Providers
        └─> Core.Utilities
```

---

## 性能建议

1. **避免重复加载**: 使用 `LoadCalibrationRecipeAsReadonlyAsync` 返回的只读对象缓存
2. **批量操作**: 如果需要批量处理多个配方，考虑使用异步并发
3. **内存释放**: 大型配方数据确保及时释放引用

---

## 调试技巧

### 检查命名空间

```csharp
// 确认已使用新的命名空间
#error "Current namespace context" // 会输出当前文件的命名空间

using CalibrationRecipe.Core.Models;  // ✅
using CalibrationRecipe.Core.Services;  // ✅
```

### 监控对象创建

```csharp
// 使用原始 DTO（可修改）
var dto = new CalibrationRecipeDto { /* ... */ };

// 创建只读包装（防止修改）
var readonlyWrapper = new CalibrationRecipeReadonlyWrapper(dto, sysInfo);

// 验证引用
Debug.Assert(ReferenceEquals(dto, readonlyWrapper.GetOriginalDto()));  // 应返回 true
```

---

## 版本管理

**CalibrationRecipe.Core** 版本与主应用同步：
- 当前版本: 与 Cuga-Calibration-Solution 一致
- 发布时: 一同构建和测试

---

**最后更新**: 2026年3月6日  
**作者**: Calibration Team  
**状态**: ✅ 完成
