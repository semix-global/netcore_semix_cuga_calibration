---
name: create-calibration-module
description: |
  参照现有 DarkAutoFocus / FAFB 补偿校准的 MVVM 结构，在 CugaCalibration 项目中新增一个完整的校准规范（Model + ViewModel + View + IOC 注册）。
  自动识别分支 Issue 编号、套用 Cache/DTO/ScatterPlotControl 模式、生成 4 步步骤流代码骨架。
---

## 触发条件

用户要求以下任一操作：
- 新增一个校准模块 / 校准规范
- 参照 DarkAutoFocus 写一个 XXX 校准
- 参照 AutoFocusFAFBCompensation 写一个校准
- 创建新的 Calibration ViewModel/View/Model

## 实现模式

以 `Sources/Cores/Core.Models/Models/AutoFocus/FAFBCompensation/` 和 `Sources/Cuga-Calibration-Solution/ViewModels/AutoFocus/AutoFocusFAFBCompensationViewModel.cs` 为模板。

### 1. 确定命名与目录

假设新校准名称为 `{Name}`，例如 `AutoFocusFAFBCompensation`：

- **Model 目录**: `Sources/Cores/Core.Models/Models/{Category}/{Name}/`
  - `{Name}Cache.cs`
  - `{Name}CacheItem.cs`
  - `{Name}DTO.cs`
  - `{Name}DTOItem.cs`
- **ViewModel**: `Sources/Cuga-Calibration-Solution/ViewModels/{Category}/{Name}ViewModel.cs`
- **ViewModel 依赖检查**: `{Name}ViewModel.LoadDepends.cs`
- **View 目录**: `Sources/Cuga-Calibration-Solution/Views/{Category}/{Name}/`
  - `{Name}UserControl.xaml`
  - `{Name}UserControl.xaml.cs`
  - `Children/Step0View.xaml`
  - `Children/Step1View.xaml`
  - `Children/Step2View.xaml`
  - `Children/Step3View.xaml`
  - `Children/Review.xaml`

### 2. Model 层规范

#### Cache

继承 `CalibrationCacheBase<T>`，包含：
- 对准结果 `AlignmentResultDto AlignmentResult`（如需对准）
- 物镜信息 `MicroscopeLensInformation MicroscopeLensInformation`（如需切换物镜）
- 业务参数（如 `StartECS`, `StopECS`, `SpeedECSPerSecond`）
- 阈值参数（如 `ThresholdZeroEcsDeviation`）
- 点位集合 `IReadOnlyList<{Name}CacheItem> Items`

#### CacheItem

继承 `ObservableObject`，包含：
- `Point Point`（机台坐标）
- 采集的原始数据数组
- 每个 Item 独立的 `IScatterPlotControl ScatterPlotControl`

#### DTO

继承 `CalibrationDTOBase<T>`，包含：
- 标定结果字段（如 `KA`, `OffsetA`, `KB`, `OffsetB`）
- `IReadOnlyList<{Name}DTOItem> Items`
- 总览 `IScatterPlotControl ScatterPlotControl`
- 实现 `Clone()` 深拷贝

#### DTOItem

包含：
- `Point Point`
- 原始曲线数据
- 计算结果（如 `ZeroEcsA`, `ZeroEcsB`, `KA`, `OffsetA`, `KB`, `OffsetB`）
- 局部 `IScatterPlotControl ScatterPlotControl`
- 实现 `ICloneable<T>`

### 3. ViewModel 层规范

#### 类定义

```csharp
[IOCAppService(ServiceType = typeof({Name}ViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class {Name}ViewModel : CalibrationViewModelBase
{
    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Alignment" },
        new() { StepName = "Select Points" },
        new() { StepName = "Acquire" },
        new() { StepName = "Verify" }
    ];

    [RecipeCache]
    [ObservableProperty]
    public partial {Name}Cache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial {Name}DTO Calibration { get; set; } = new();

    [ObservableProperty]
    public partial {Name}DTO CalibratingItem { get; set; } = new();

    [ObservableProperty]
    public partial {Name}DTO Review { get; set; } = new();

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = HostApplication.GetRequiredService<AlignmentUserControlViewModel>();
}
```

#### 生命周期方法

- `LoadedingAsync`: 依赖检查、读取 Cache 与 Calibration、初始化默认物镜
- `CalibratingAsync`: 默认返回 `true`
- `ReviewingAsync`: `Review = Calibration.Clone(); return Review.IsCalibrated;`
- `NextingAsync`: 根据 `CalibrationStepIndex` 执行步骤切换逻辑；最后一步保存 DTO
- `PreviousingAsync`: 回退时切回物镜

#### 校准命令

- `Step0CalibrateActionCommand`: 调用 `AlignmentUserControlViewModel.AlignmentAsync`，保存结果到 `Cache.AlignmentResult`
- `Step1CalibrateActionCommand`: 参数校验（物镜、Productivity、ECS 范围等）
- `Step2CalibrateActionCommand`: 遍历点位执行业务采集
- `Step3CalibrateActionCommand`: 验证复采
- `VerifyActionCommand`: Review 页复采并保存验证结果

#### 点位管理命令

- `AddPoint`: 取当前 `StageViewModel.GetMachineStagePosition()` 加入 `Cache.Items`
- `RemovePoint`: 移除 `SelectedCacheItem`
- `ClearPoints`: 清空

### 4. View 层规范

#### UserControl

- `x:Class="CugaCalibration.Views.{Category}.{Name}.{Name}UserControl"`
- `xmlns:children="clr-namespace:CugaCalibration.Views.{Category}.{Name}.Children"`
- `AutoWireViewModelHelper.IsAutoWireViewModel="True"`
- 使用 `ContentControl` + `DataTrigger` 根据 `CalibrationStepIndex` 切换 `Step0View`~`Step3View` 和 `Review`

#### Code-Behind

```csharp
[Permission]
public partial class {Name}UserControl : UserControl
{
    public {Name}UserControl()
    {
        InitializeComponent();
        InitializePermissionControl();
    }
}
```

#### Step0View

- DSW 对准界面
- 选择明场/暗场（可选）
- 显示 `AlignmentResult`
- Action / Cancel 按钮

#### Step1View

- 显示/编辑 `Cache.Items`（DataGrid/ListView）
- Add / Remove / Clear 按钮
- 参数输入：`StartECS`, `StopECS`, `SpeedECSPerSecond`, `Threshold...`

#### Step2View

- 进度显示
- 当前点位曲线 `ContentControl Content="{Binding SelectedCalibratingItem.ScatterPlotControl}"`
- Action 按钮

#### Step3View

- 复采验证界面，显示偏差结果

#### Review

- 显示 `Review.KA/OffsetA...` 与总览图
- Verify Action 按钮

### 5. IOC / 权限 / 菜单

- ViewModel 标注 `[IOCAppService]`
- UserControl code-behind 标注 `[Permission]` 并在构造函数调用 `InitializePermissionControl()`
- 数据库 `SysMenu` 新增一行，`Component` 为完整 ViewModel 类型名（由实施人员/DB 脚本处理）

### 6. 常用 API

#### S 曲线采集

```csharp
StageViewModel.SetCalChipDswDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(point));
CIBViewModel.ToggleRTFCParam(ApplicationCookie.OILowProductivityInformation);
AfViewModel.ToggleDarkFieldEnable(true);
await Task.Delay(100, cancellationToken);
AfViewModel.SetSensorEcsValue(Cache.StartECS);
await Task.Delay(100, cancellationToken);

var traceBufferList = AfViewModel.GetSensorNscTraceBufferList(
    Cache.StartECS,
    Cache.StopECS,
    Cache.SpeedECSPerSecond,
    TimeSpan.FromSeconds(Math.Abs(Cache.StopECS - Cache.StartECS) / Cache.SpeedECSPerSecond + 2));

var ecses = traceBufferList.Select(t => t.Ecs).ToArray();
var fas = traceBufferList.Select(t => t.Fa).ToArray();
```

#### 零点查找

在极值区间内，取最接近 0 的正负端点 ECS 平均值。

#### 补偿系数

对区间做一元线性拟合 `FA = m * ECS + b`：
- `K = 1 / m`
- `Offset = -b / m`

### 7. 执行步骤

1. 与用户确认校准名称 `{Name}`、所属类别 `{Category}`、业务步骤数（默认 4 步）。
2. 在 `Core.Models` 创建 Model 目录与 4 个文件。
3. 在 `Cuga-Calibration-Solution` 创建 ViewModel 与 `LoadDepends`。
4. 在 `Cuga-Calibration-Solution` 创建 View 目录与所有 xaml/xaml.cs。
5. 补齐必要的 `using`、修正 ScottPlot API 签名、处理 wpftmp 编译问题。
6. `dotnet build` 编译验证。
7. 按项目 Git 规范编写 commit message，前缀 `#number `。
8. 尝试 `git push`；失败则停止并询问用户。

### 8. 注意事项

- 新增文件必须引入 `using Net.Utilities.WPF.MVVM;` 才能使用 `HostApplication`。
- ScottPlot 颜色应使用 `Constants.Category10.GetColor(index)`，不要直接传 `ScottPlot.Color` 给 `GetOrAddScatterLine` 的第 5 参数（该参数是 `int colorIndex`）。
- DTO 中引用 `Point` 需 `using Net.Utilities.Models.Geometries;`。
- 使用 `Rows`/`Columns` 布局需 `using ScottPlot.MultiplotLayouts;`。
- `GetAllHtmlPlot2DLinesCharts` 扩展需 `using Net.Utilities.ScottPlot.WPF.Extensions;`。
- 属性变更触发绘图应使用 `partial void OnXxxChanged(...)`。
- 不要在校准 Cache 中放置可由 `ApplicationCookie` 获取的全局选择项（如 `ProductivityInformation`）。
