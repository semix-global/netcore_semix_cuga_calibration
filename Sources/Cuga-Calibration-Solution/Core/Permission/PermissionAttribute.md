# PermissionAttribute 权限注解文档

## 概述

`PermissionAttribute` 是一个基于 AOP（面向切面编程）的自定义权限控制注解，利用 [MrAdvice](https://github.com/ArxOne/MrAdvice) 库实现方法拦截，用于在 WPF 应用程序中实现细粒度的 UI 权限控制。

## 技术原理

### 核心依赖

- **MrAdvice (v2.19.1)**: 轻量级的 .NET AOP 框架，通过 IL 织入（IL Weaving）在编译时修改程序集，实现方法拦截。

### 工作流程

```
┌─────────────────────────────────────────────────────────────────┐
│                    [Permission] 注解执行流程                     │
├─────────────────────────────────────────────────────────────────┤
│  1. 构造函数被调用                                               │
│     ↓                                                           │
│  2. MrAdvice 拦截方法执行                                        │
│     ↓                                                           │
│  3. Advise 方法被调用                                            │
│     ↓                                                           │
│  4. context.Proceed() 执行原始构造函数（InitializeComponent）    │
│     ↓                                                           │
│  5. OnExit 方法处理权限逻辑                                       │
│     ↓                                                           │
│  6. 根据用户权限修改 UI 控件状态                                  │
└─────────────────────────────────────────────────────────────────┘
```

## 属性定义

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
public sealed class PermissionAttribute : System.Attribute, IMethodAdvice
```

### 特性说明

| 属性 | 说明 |
|------|------|
| **作用目标** | 方法（Method）和构造函数（Constructor） |
| **接口实现** | `IMethodAdvice` - MrAdvice 提供的方法拦截接口 |
| **执行时机** | 方法执行后（OnExit） |

## 权限控制类型

`PermissionAttribute` 支持三种权限控制类型，由 `MenuTypeEnum` 枚举定义：

| 类型 | 枚举值 | 控制行为 | 影响属性 |
|------|--------|----------|----------|
| **只读** | `Readable` | 将控件设为只读状态 | `IsReadOnly = true` |
| **隐藏** | `Visible` | 隐藏控件 | `Visibility = Collapsed` |
| **禁用** | `Enable` | 禁用控件交互 | `IsEnabled = false` |

## 使用方式

### 基本用法

在 UserControl 的构造函数上添加 `[Permission]` 注解：

```csharp
using CugaCalibration.Core.Attribute;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Views.Laser.AutoFocus;

[IOCAppService(ServiceType = typeof(LaserAutoFocusCalibrationUserControl), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserAutoFocusCalibrationUserControl
{
    [Permission]
    public LaserAutoFocusCalibrationUserControl()
    {
        InitializeComponent();
    }
}
```

### 使用场景汇总

当前项目中，`[Permission]` 注解被广泛应用于各个校准模块的 UserControl：

#### AOD 模块
- `AODAlignmentUserControl`
- `AODDelayUserControl`

#### ADS 模块
- `AdsXGainsCalibrationUserControl`
- `AdsYGainsCalibrationUserControl`
- `AdsPressureGainsCalibrationUserControl`

#### CIB 模块
- `CIBMMDUserControl`
- `CIBXPixelSizeUserControl`
- `CIBLightMatchingUserControl`
- `CIBIlluminationProfileUserControl`

#### Chuck 模块
- `ChuckPrealignerCalibrationUserControl`
- `ChuckGantryCalibrationUserControl`
- `ChuckCenterAndThetaCalibrationUserControl`
- `ChuckGlobalScaleErrorCalibrationUserControl`
- `ChuckAlignmentDegreeOffsetCalibrationUserControl`

#### Laser 模块
- `LaserAutoFocusCalibrationUserControl`
- `LaserPixelSizeCalibrationUserControl`
- `LaserLineCentricityCalibrationUserControl`
- `LaserLineOrientationOffsetCalibrationUserControl`
- `LaserXYAstigmatismCalibrationUserControl`
- `LaserBeamStabilizerCalibrationUserControl`
- `LaserDOEAngleCalibrationUserControl`
- `LaserAttenuatorUserControl`
- `LaserOpticalPowerMeterUserControl`

#### Microscope 模块
- `MicroscopePixelSizeCalibrationUserControl`
- `MicroscopeFocusCalibrationUserControl`
- `MicroscopeCentricityCalibrationUserControl`
- `MicroscopeCalChipCalibrationUserControl`

#### Optics 模块
- `OpticsRelayUserControl`
- `OpticsINCUserControl`

## 权限验证逻辑

### 关键组件

1. **ApplicationCookie**: 存储当前用户信息和权限菜单列表
2. **IApplicationCookieService**: 提供权限查询服务

### 验证流程

```csharp
private static void OnExit(MethodAdviceContext context)
{
    // 1. 检查目标是否为 FrameworkElement
    if (context.Target is not FrameworkElement frameworkElement) return;
    if (frameworkElement.DataContext is null) return;

    // 2. 获取权限服务
    var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();
    var applicationCookieService = HostApplication.GetRequiredService<IApplicationCookieService>();

    // 3. 管理员跳过权限检查
    if (applicationCookie.SysUser.IsAdmin) return;

    // 4. 递归查找当前组件关联的菜单权限配置
    var list = applicationCookieService.FindSysMenuListByRecursionComponent(
        frameworkElement.DataContext.GetType().FullName!
    );

    // 5. 遍历权限配置，修改无权限控件的状态
    foreach (var detail in list.Where(t => 
        string.IsNullOrWhiteSpace(t.Component) == false && 
        string.IsNullOrWhiteSpace(t.Perms) == false))
    {
        // 有权限的不处理
        if (applicationCookie.RoleSysMenuList.Any(t => t.Id == detail.Id)) continue;

        // 查找并修改控件状态
        // ...
    }
}
```

### 权限数据结构

权限配置通过 `SysMenuDTO` 实体管理：

| 字段 | 说明 |
|------|------|
| `Component` | ViewModel 的完整类型名称 |
| `Perms` | 控件名称（XAML 中的 `x:Name`） |
| `MenuTypeEnum` | 权限控制类型（Readable/Visible/Enable） |

## 配置权限

在菜单管理界面（`MenusManagementUserControl`）中可配置：

1. **Menu Name**: 菜单名称
2. **Perms**: 控件权限标识（对应 XAML 中控件的 `x:Name`）
3. **Component**: 关联的 ViewModel 类型全名
4. **Menu Type**: 权限类型
5. **EnableStatus**: 启用状态

## 注意事项

1. **管理员免检**: 如果当前用户 `IsAdmin` 为 `true`，将跳过所有权限检查
2. **DataContext 依赖**: 权限检查依赖 `FrameworkElement.DataContext`，确保在构造函数执行后 DataContext 已绑定
3. **控件命名**: XAML 中需要权限控制的控件必须设置 `x:Name`，且与 `SysMenuDTO.Perms` 匹配
4. **IOC 单例**: 建议将 UserControl 注册为单例，避免重复的权限验证

## 扩展建议

如需扩展新的权限控制类型，可在 `MenuTypeEnum` 中添加新枚举值，并在 `OnExit` 方法的 `switch` 语句中添加对应处理逻辑。

## 相关文件

- 属性定义: `Core/Attribute/PromissAttribute.cs`
- 权限服务接口: `Core/Services/Interfaces/IApplicationCookieService.cs`
- 权限服务实现: `Core/Services/Implements/ApplicationCookieServiceImpl.cs`

---

# 迁移计划：从 MrAdvice IL织入 迁移到 Source Generator

## 一、背景与动机

### 1.1 当前实现的问题

| 问题 | 描述 |
|------|------|
| **性能开销** | MrAdvice 使用 IL 织入在运行时修改方法，每次方法调用都有额外开销 |
| **编译时间** | IL 织入需要在编译后处理程序集，增加构建时间 |
| **调试困难** | 织入后的代码难以调试，堆栈跟踪不清晰 |
| **依赖风险** | 依赖第三方 AOP 框架，维护和兼容性风险 |
| **AOT 不兼容** | IL 织入与 .NET Native AOT 编译不兼容 |

### 1.2 Source Generator 优势

| 优势 | 描述 |
|------|------|
| **零运行时开销** | 编译时生成代码，无反射、无代理 |
| **可调试** | 生成的代码可见、可调试 |
| **IDE 支持** | 完整的智能提示和错误检查 |
| **AOT 兼容** | 与 .NET 8+ AOT 完全兼容 |
| **官方支持** | 微软官方特性，长期维护保证 |

## 二、技术方案设计

### 2.1 方案对比

```
┌────────────────────────────────────────────────────────────────────────┐
│                         方案选择分析                                    │
├────────────────────────────────────────────────────────────────────────┤
│  方案A: Partial Method 生成                                            │
│  ├─ 优点: 代码改动最小，保持构造函数简洁                                 │
│  ├─ 缺点: 需要手动调用生成的方法                                        │
│  └─ 适用: 简单场景                                                     │
│                                                                        │
│  方案B: Behavior/Attached Property 生成 ⭐ 推荐                         │
│  ├─ 优点: XAML 声明式、符合 WPF 最佳实践、无侵入性                       │
│  ├─ 缺点: 需要修改 XAML 文件                                            │
│  └─ 适用: WPF 应用程序                                                 │
│                                                                        │
│  方案C: 生成包装类                                                      │
│  ├─ 优点: 完全解耦                                                     │
│  ├─ 缺点: 代码复杂度高                                                 │
│  └─ 适用: 复杂企业应用                                                 │
└────────────────────────────────────────────────────────────────────────┘
```

### 2.2 推荐方案：Partial Class + 自动注入 Loaded 事件

采用 **混合方案**：Source Generator 生成 partial class 的扩展部分，自动注入权限检查逻辑到 `Loaded` 事件。

#### 设计架构

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     Source Generator 架构                                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────────┐     编译时      ┌─────────────────────────┐   │
│  │  [Permission]       │ ──────────────► │  Roslyn Analyzer        │   │
│  │  标记的 UserControl │                 │  扫描带标记的类          │   │
│  └─────────────────────┘                 └───────────┬─────────────┘   │
│                                                      │                  │
│                                                      ▼                  │
│                                          ┌─────────────────────────┐   │
│                                          │  Source Generator       │   │
│                                          │  生成 partial class     │   │
│                                          └───────────┬─────────────┘   │
│                                                      │                  │
│                                                      ▼                  │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Generated Code:                                                 │   │
│  │  ┌─────────────────────────────────────────────────────────┐    │   │
│  │  │  partial class XxxUserControl                            │    │   │
│  │  │  {                                                       │    │   │
│  │  │      private void InitializePermission()                 │    │   │
│  │  │      {                                                   │    │   │
│  │  │          this.Loaded += OnPermissionLoaded;              │    │   │
│  │  │      }                                                   │    │   │
│  │  │                                                          │    │   │
│  │  │      private void OnPermissionLoaded(...)                │    │   │
│  │  │      {                                                   │    │   │
│  │  │          PermissionHelper.ApplyPermissions(this);        │    │   │
│  │  │      }                                                   │    │   │
│  │  │  }                                                       │    │   │
│  │  └─────────────────────────────────────────────────────────┘    │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## 三、详细实施计划

### Phase 1: 准备阶段（1-2 天）

#### 1.1 创建 Source Generator 项目

```
Sources/
├── Cores/
│   ├── Core.Utilities.SourceGenerators/           # 新建项目
│   │   ├── Core.Utilities.SourceGenerators.csproj
│   │   ├── PermissionGenerator.cs       # 主生成器
│   │   ├── PermissionSyntaxReceiver.cs  # 语法接收器
│   │   └── Templates/
│   │       └── PermissionPartial.txt    # 代码模板
│   └── ...
```

#### 1.2 项目配置

**Core.Utilities.SourceGenerators.csproj**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>netstandard2.0</TargetFramework>
        <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
        <IsRoslynComponent>true</IsRoslynComponent>
        <LangVersion>latest</LangVersion>
    </PropertyGroup>
    
    <ItemGroup>
        <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" PrivateAssets="all" />
        <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
    </ItemGroup>
</Project>
```

#### 1.3 定义新的标记特性

创建一个新的、简洁的标记特性（不依赖 MrAdvice）：

```csharp
// Core.Models/Attributes/PermissionAttribute.cs
namespace Core.Models.Attributes;

/// <summary>
/// 标记需要权限控制的 UserControl
/// Source Generator 将自动生成权限检查代码
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PermissionAttribute : Attribute
{
}
```

### Phase 2: Source Generator 开发（3-4 天）

#### 2.1 语法接收器实现

```csharp
// PermissionSyntaxReceiver.cs
public class PermissionSyntaxReceiver : ISyntaxContextReceiver
{
    public List<ClassDeclarationSyntax> CandidateClasses { get; } = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is ClassDeclarationSyntax classDeclaration)
        {
            // 检查是否有 [Permission] 特性
            if (HasPermissionControlAttribute(classDeclaration, context.SemanticModel))
            {
                CandidateClasses.Add(classDeclaration);
            }
        }
    }
}
```

#### 2.2 主生成器实现

```csharp
// PermissionGenerator.cs
[Generator]
public class PermissionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. 收集带有 [Permission] 的类
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsCandidateClass(s),
                transform: static (ctx, _) => GetSemanticTarget(ctx))
            .Where(static m => m is not null);

        // 2. 生成代码
        context.RegisterSourceOutput(classDeclarations, 
            static (spc, source) => Execute(source, spc));
    }

    private static void Execute(ClassInfo classInfo, SourceProductionContext context)
    {
        // 生成 partial class 代码
        var source = GeneratePartialClass(classInfo);
        context.AddSource($"{classInfo.ClassName}.Permission.g.cs", source);
    }
}
```

#### 2.3 生成的代码模板

```csharp
// 生成的代码示例: LaserAutoFocusCalibrationUserControl.Permission.g.cs
// <auto-generated />
#nullable enable

namespace CugaCalibration.Views.Laser.AutoFocus;

partial class LaserAutoFocusCalibrationUserControl
{
    /// <summary>
    /// 初始化权限控制（由 Source Generator 自动生成）
    /// </summary>
    private void InitializePermissionControl()
    {
        this.Loaded += OnPermissionControlLoaded;
    }

    private void OnPermissionControlLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        // 避免重复执行
        this.Loaded -= OnPermissionControlLoaded;
        
        // 应用权限
        global::CugaCalibration.Core.Permission.PermissionHelper.ApplyPermissions(this);
    }
}
```

### Phase 3: 权限辅助类重构（1-2 天）

#### 3.1 创建静态权限辅助类

将原有 `PermissionAttribute.OnExit` 逻辑提取为静态辅助类：

```csharp
// Core/Permission/PermissionHelper.cs
namespace CugaCalibration.Core.Permission;

public static class PermissionHelper
{
    /// <summary>
    /// 应用权限控制到指定的 FrameworkElement
    /// </summary>
    public static void ApplyPermissions(FrameworkElement frameworkElement)
    {
        if (frameworkElement.DataContext is null) return;

        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();
        var applicationCookieService = HostApplication.GetRequiredService<IApplicationCookieService>();

        if (applicationCookie.SysUser.IsAdmin) return;

        var componentName = frameworkElement.DataContext.GetType().FullName!;
        var list = applicationCookieService.FindSysMenuListByRecursionComponent(componentName);
        
        foreach (var detail in list.Where(t => 
            !string.IsNullOrWhiteSpace(t.Component) && 
            !string.IsNullOrWhiteSpace(t.Perms)))
        {
            if (applicationCookie.RoleSysMenuList.Any(t => t.Id == detail.Id)) continue;

            ApplyPermissionToControl(frameworkElement, detail);
        }
    }

    private static void ApplyPermissionToControl(FrameworkElement root, SysMenuDTO detail)
    {
        var type = Type.GetType(detail.Component);
        if (type is null) return;

        foreach (var descendant in DependencyObjectHelper
            .FindVisualDescendants(root, type)
            .OfType<FrameworkElement>())
        {
            var control = descendant.FindName(detail.Perms!);
            if (control is null) continue;

            ApplyPermissionType(control, detail.MenuTypeEnum);
        }
    }

    private static void ApplyPermissionType(object control, MenuTypeEnum menuType)
    {
        switch (menuType)
        {
            case MenuTypeEnum.Readable:
                SetPropertyIfExists(control, "IsReadOnly", true);
                break;
            case MenuTypeEnum.Visible:
                SetPropertyIfExists(control, "Visibility", Visibility.Collapsed);
                break;
            case MenuTypeEnum.Enable:
                SetPropertyIfExists(control, "IsEnabled", false);
                break;
        }
    }

    private static void SetPropertyIfExists(object target, string propertyName, object value)
    {
        var property = target.GetType().GetProperty(propertyName);
        if (property?.CanWrite == true && property.PropertyType.IsInstanceOfType(value))
        {
            property.SetValue(target, value);
        }
    }
}
```

### Phase 4: 迁移现有代码（2-3 天）

#### 4.1 批量迁移脚本

需要修改 29 个 UserControl 文件，迁移步骤：

**Before（旧代码）**:
```csharp
using CugaCalibration.Core.Attribute;

[IOCAppService(...)]
public sealed partial class LaserAutoFocusCalibrationUserControl
{
    [Permission]
    public LaserAutoFocusCalibrationUserControl()
    {
        InitializeComponent();
    }
}
```

**After（新代码）**:
```csharp
using Core.Models.Attributes;

[IOCAppService(...)]
[Permission]  // 类级别标记
public sealed partial class LaserAutoFocusCalibrationUserControl
{
    public LaserAutoFocusCalibrationUserControl()
    {
        InitializeComponent();
        InitializePermissionControl();  // 调用生成的方法
    }
}
```

#### 4.2 需要迁移的文件清单

| 模块 | 文件 |
|------|------|
| AOD | `AODAlignmentUserControl.xaml.cs`, `AODDelayUserControl.xaml.cs` |
| ADS | `AdsXGainsCalibrationUserControl.xaml.cs`, `AdsYGainsCalibrationUserControl.xaml.cs`, `AdsPressureGainsCalibrationUserControl.xaml.cs` |
| CIB | `CIBMMDUserControl.xaml.cs`, `CIBXPixelSizeUserControl.xaml.cs`, `CIBLightMatchingUserControl.xaml.cs`, `CIBIlluminationProfileUserControl.xaml.cs` |
| Chuck | `ChuckPrealignerCalibrationUserControl.xaml.cs`, `ChuckGantryCalibrationUserControl.xaml.cs`, `ChuckCenterAndThetaCalibrationUserControl.xaml.cs`, `ChuckGlobalScaleErrorCalibrationUserControl.xaml.cs`, `ChuckAlignmentDegreeOffsetCalibrationUserControl.xaml.cs` |
| Laser | `LaserAutoFocusCalibrationUserControl.xaml.cs`, `LaserPixelSizeCalibrationUserControl.xaml.cs`, `LaserLineCentricityCalibrationUserControl.xaml.cs`, `LaserLineOrientationOffsetCalibrationUserControl.xaml.cs`, `LaserXYAstigmatismCalibrationUserControl.xaml.cs`, `LaserBeamStabilizerCalibrationUserControl.xaml.cs`, `LaserDOEAngleCalibrationUserControl.xaml.cs`, `LaserAttenuatorUserControl.xaml.cs`, `LaserOpticalPowerMeterUserControl.xaml.cs` |
| Microscope | `MicroscopePixelSizeCalibrationUserControl.xaml.cs`, `MicroscopeFocusCalibrationUserControl.xaml.cs`, `MicroscopeCentricityCalibrationUserControl.xaml.cs`, `MicroscopeCalChipCalibrationUserControl.xaml.cs` |
| Optics | `OpticsRelayUserControl.xaml.cs`, `OpticsINCUserControl.xaml.cs` |

### Phase 5: 测试与验证（2-3 天）

#### 5.1 单元测试

```csharp
[TestClass]
public class PermissionGeneratorTests
{
    [TestMethod]
    public void Generator_ShouldGeneratePartialClass_WhenAttributePresent()
    {
        // Arrange
        var source = @"
            using Core.Models.Attributes;
            
            [Permission]
            public partial class TestUserControl : UserControl { }
        ";
        
        // Act
        var result = GeneratorTestHelper.RunGenerator(source);
        
        // Assert
        Assert.IsTrue(result.GeneratedSources.Any(s => 
            s.HintName.Contains("TestUserControl.Permission.g.cs")));
    }
}
```

#### 5.2 集成测试

- 验证权限控制功能在各模块正常工作
- 验证管理员跳过权限检查
- 验证三种权限类型（Readable/Visible/Enable）

### Phase 6: 清理与优化（1 天）

#### 6.1 移除 MrAdvice 依赖

```xml
<!-- Cuga-Calibration-Solution.csproj -->
<ItemGroup>
    <!-- 删除以下行 -->
    <!-- <PackageReference Include="MrAdvice" /> -->
</ItemGroup>
```

#### 6.2 删除旧文件

- 删除 `Core/Attribute/PromissAttribute.cs`
- 更新 `PermissionAttribute.md` 文档

## 四、时间计划

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         实施时间表                                       │
├─────────────┬───────────┬───────────────────────────────────────────────┤
│    阶段     │   时间    │                   任务                         │
├─────────────┼───────────┼───────────────────────────────────────────────┤
│  Phase 1    │  1-2 天   │ 创建 Source Generator 项目，配置基础设施        │
│  Phase 2    │  3-4 天   │ 实现 Source Generator 核心逻辑                 │
│  Phase 3    │  1-2 天   │ 重构权限辅助类                                 │
│  Phase 4    │  2-3 天   │ 迁移 29 个 UserControl                        │
│  Phase 5    │  2-3 天   │ 测试与验证                                     │
│  Phase 6    │  1 天     │ 清理与优化                                     │
├─────────────┼───────────┼───────────────────────────────────────────────┤
│   总计      │ 10-15 天  │                                               │
└─────────────┴───────────┴───────────────────────────────────────────────┘
```

## 五、风险与缓解措施

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| Source Generator 不支持 .NET Framework 4.8 | 高 | 使用 `netstandard2.0` 目标框架编写生成器，生成的代码兼容 net480 |
| 生成代码与手写代码冲突 | 中 | 使用 partial class，确保方法名唯一（加 `_Generated` 后缀） |
| 调试困难 | 中 | 添加 `#nullable enable` 和详细注释，支持 Source Link |
| 迁移期间功能中断 | 高 | 分模块迁移，保持双轨运行直到验证通过 |

## 六、预期收益

| 指标 | 优化前 | 优化后 | 提升 |
|------|--------|--------|------|
| 构建时间 | 较长（IL织入） | 更快 | ~20-30% |
| 运行时性能 | 有代理开销 | 零开销 | ~50%+ |
| 代码可读性 | 织入后不可见 | 完全可见 | 显著提升 |
| 调试体验 | 困难 | 原生支持 | 显著提升 |
| AOT 兼容性 | 不兼容 | 完全兼容 | 从 0 到 100% |

## 七、备选方案

如果 Source Generator 方案遇到困难，可考虑以下备选方案：

### 方案 B: 使用 WPF Behavior

```xml
<UserControl>
    <b:Interaction.Behaviors>
        <permission:PermissionBehavior />
    </b:Interaction.Behaviors>
</UserControl>
```

### 方案 C: 使用基类封装

```csharp
public abstract class PermissionUserControl : UserControl
{
    protected PermissionUserControl()
    {
        Loaded += OnPermissionLoaded;
    }
    
    private void OnPermissionLoaded(object sender, RoutedEventArgs e)
    {
        PermissionHelper.ApplyPermissions(this);
    }
}
```

## 八、下一步行动

1. ✅ 完成本计划文档
2. ⏳ 创建 `Core.Utilities.SourceGenerators` 项目
3. ⏳ 实现基础的 Source Generator
4. ⏳ 编写单元测试验证生成代码
5. ⏳ 试点迁移一个模块（建议从 AOD 开始）
6. ⏳ 全面迁移并验证
