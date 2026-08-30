# Optics Fourier Image Viewer ROI 编辑器与 StageMap 说明

## 1. 文档范围

本文记录当前项目中 Rect ROI 编辑器、Demo 测试程序的设计和实现，同时记录 `StageMapWindowViewModel.ProcessStage2Residuals` 报错的定位结果。

ROI 编辑器相关代码位于：

- `Sources/Net.Utilities/Net.Utilities.OpticsFourierImageViewer.WPF/Drawables/RectROIDrawable.cs`
- `Sources/Net.Utilities/Net.Utilities.OpticsFourierImageViewer.WPF/Editors/AddOrModifyRectROIDrawableInputOptions.cs`
- `Sources/Net.Utilities/Net.Utilities.OpticsFourierImageViewer.WPF/Editors/AddOrModifyRectROIDrawableGetterEditor.cs`
- `Sources/Net.Utilities/Net.Utilities.OpticsFourierImageViewer.WPF/Primitives/Enums/EditorStateEnum.cs`
- `Sources/Net.Utilities/Net.Utilities.OpticsFourierImageViewer.WPF/Primitives/Enums/RectROIDrawableControlPointTypesEnum.cs`

Demo 代码位于：

- `Sources/Demos/OpticsFourierImageViewerTest/MainWindowViewModel.cs`
- `Sources/Demos/OpticsFourierImageViewerTest/MainWindow.xaml`

StageMap 代码位于：

- `Sources/Cuga-Calibration-Solution/ViewModels/Common/Windows/Tools/Stage/StageMapWindowViewModel.cs`
- `Sources/Cuga-Calibration-Solution/ViewModels/Common/Windows/Tools/Stage/StageMap.cs`
- `Sources/Cuga-Calibration-Solution/Assets/Python/closed_loop_calibration.py`

## 2. ROI 编辑器设计目标

编辑器是一个“选择 → 修改”的持续编辑器，只处理已经存在的 `RectROIDrawable`，不负责创建新的 ROI。

核心规则如下：

1. ROI 必须属于当前项目的 `Net.Utilities.OpticsFourierImageViewer.WPF.Drawables.RectROIDrawable` 类型。
2. 编辑器必须传入 `BitmapImageDrawable`，所有 ROI 都不能超出该图片范围。
3. 支持单击选择、Ctrl 多选、Ctrl 取消选择和框选。
4. 框选只要 ROI 与选择框相交就会被选中，不再区分正向和反向框选。
5. 拖拽一个已选 ROI 的本体或锚点时，所有已选 ROI 同步处理。
6. Enter 表示成功，保留修改；Escape、取消、超时和外部取消都会回滚。

## 3. Input Options

`AddOrModifyRectROIDrawableInputOptions` 当前定义如下：

```csharp
public sealed class AddOrModifyRectROIDrawableInputOptions : InputOptions<Unit>
{
    public BitmapImageDrawable BitmapImageDrawable { get; }

    public bool IsEnableDragMove { get; set; } = true;
}
```

### 3.1 BitmapImageDrawable

构造函数必须传入 `BitmapImageDrawable`：

```csharp
var options = new AddOrModifyRectROIDrawableInputOptions(BitmapImageDrawable);
```

编辑器根据以下信息计算图片范围：

```text
左下角 = BitmapImageDrawable.Point
宽度   = BitmapImage.Width
高度   = BitmapImage.Height
```

图片为空或未加载时，编辑器初始化失败，不会进入编辑循环。

### 3.2 IsEnableDragMove

`IsEnableDragMove` 默认值为 `true`。

- `true`：允许拖拽已选 ROI 本体移动。
- `false`：禁止拖拽 ROI 本体移动，但仍允许拖拽锚点缩放。

该开关只控制本体移动，不影响选择、Ctrl 多选、Ctrl 取消选择和锚点缩放。

## 4. 编辑器状态

本地编辑器使用自己的状态枚举：

```csharp
namespace Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;

public enum EditorStateEnum
{
    Select,
    Modify
}
```

状态含义：

| 状态 | 含义 |
| --- | --- |
| `Select` | 等待点选或框选 ROI |
| `Modify` | 已有 ROI 被选中，可以移动或缩放 |

外部 `Net.Utilities.ImageViewer.WPF` 命名空间中的旧 `EditorStateEnum` 不删除，本地光学项目不再依赖它。

## 5. ROI 锚点 Flags

锚点枚举与 `Rect` 的属性名称保持一致：

```csharp
[Flags]
public enum RectROIDrawableControlPointTypesEnum
{
    None = 0,
    XMaxYMax = 1 << 0,
    XMinYMax = 1 << 1,
    XMinYMin = 1 << 2,
    XMaxYMin = 1 << 3,
    XCenterYMax = 1 << 4,
    XMinYCenter = 1 << 5,
    XCenterYMin = 1 << 6,
    XMaxYCenter = 1 << 7,
    All = XMaxYMax | XMinYMax | XMinYMin | XMaxYMin |
          XCenterYMax | XMinYCenter | XCenterYMin | XMaxYCenter
}
```

八个锚点的含义：

| 名称 | 位置 |
| --- | --- |
| `XMaxYMax` | 右上角 |
| `XMinYMax` | 左上角 |
| `XMinYMin` | 左下角 |
| `XMaxYMin` | 右下角 |
| `XCenterYMax` | 上边中点 |
| `XMinYCenter` | 左边中点 |
| `XCenterYMin` | 下边中点 |
| `XMaxYCenter` | 右边中点 |

`RectROIDrawable.ControlPointTypesEnum` 默认通过八个成员按位或得到全部锚点。`None` 不返回锚点，单个或多个 Flags 只返回对应锚点，`All` 返回全部八个锚点。

锚点的名称、Flags 和坐标由 `RectROIDrawable` 内部同一份定义生成，避免出现“显示位置是一个锚点，但名称是另一个锚点”的问题。

渲染引擎的 `CanvasView` 已经遍历 `GetControlPoints()` 绘制锚点，因此不需要修改外部渲染循环。

## 6. IsModified

`RectROIDrawable` 增加：

```csharp
public bool IsModified { get; set; }
```

默认值为 `false`。当 `Rect` 属性发生变化时，通过 CommunityToolkit 生成属性的 partial 回调统一设置为 `true`：

```csharp
partial void OnRectChanged(Rect value) => IsModified = true;
```

初始化 Demo ROI 后需要显式设置：

```csharp
IsModified = false
```

取消编辑时同时恢复 `Rect` 和 `IsModified`，避免取消操作留下修改标记。

## 7. 选择行为

### 7.1 普通点选

- 点击未选中的 ROI：清空旧选择，只选择当前 ROI。
- 点击空白区域：开始框选流程。
- 点击已选 ROI：进入修改流程。
- 点击已选 ROI 的锚点：进入锚点缩放流程。

### 7.2 Ctrl 点选

Ctrl 点击采用切换语义：

- Ctrl 点击未选中的 ROI：加入选择。
- Ctrl 点击已选中的 ROI：取消选择。
- 取消最后一个 ROI 后，编辑器返回 `Select` 状态。

`Edit.SelectedItems` 与每个 ROI 的 `IsSelected` 始终同步，保证 `CanvasView` 能正确绘制选中效果和锚点。

### 7.3 框选

框选使用 `SelectionWindow` 作为临时对象。

框选结束后，所有可见的 `RectROIDrawable` 中满足以下条件的对象都会被选中：

```csharp
selectionWindowExtents.IntersectsWith(rectROIDrawable.GetExtents())
```

也就是说：

- ROI 完全位于选择框内部：选中。
- ROI 与选择框部分相交：选中。
- ROI 与选择框不相交：不选中。
- 从左向右和从右向左拖动：行为相同。
- Ctrl 框选：对相交到的 ROI 逐个切换选择状态。
- 普通框选：替换原有选择。

## 8. 修改行为

### 8.1 多选移动

拖拽任意已选 ROI 的本体时：

1. 保存所有选中 ROI 的拖拽开始矩形。
2. 使用鼠标产生的同一个位移向量。
3. 根据所有选中 ROI 的整体范围计算共同合法位移。
4. 所有 ROI 同步平移，保持彼此相对位置不变。

### 8.2 多选缩放

拖拽任意一个已选 ROI 的锚点时：

1. 根据被拖拽锚点名称确定缩放方向。
2. 所有选中 ROI 使用同一个锚点方向。
3. 所有选中 ROI 使用同一个鼠标拖动向量。
4. 每个 ROI 单独限制到图片范围内。

多选缩放不是对全部 ROI 的外接框进行比例缩放，而是对每个 ROI 应用同一个锚点和拖动向量。

### 8.3 图片边界

初始化、移动和缩放都经过统一的 `ClampRectToImage` 处理，保证：

```csharp
imageRect.Contains(roiRect)
```

移动时限制的是所有选中 ROI 的共同位移；缩放时限制每个 ROI 的结果。ROI 坐标继续使用画布笛卡尔坐标。

## 9. 键盘和结束行为

### Enter

`KeyDownInput` 捕获 Enter：

- 返回 `Unit.Default`。
- 标记编辑成功结束。
- 基础编辑器清理时不回滚 ROI。

### Escape、取消、超时和外部取消

Escape 留给基础 `GetterEditor` 生成取消结果，`CancelInput` 负责：

1. 恢复所有 ROI 的原始 `Rect`。
2. 恢复所有 ROI 的原始 `IsModified`。
3. 删除临时选择框。
4. 清理鼠标拖拽和缩放状态。
5. 清理选中项。

## 10. Demo 测试程序

### 10.1 打开图片

打开图片成功后：

1. 更新 `BitmapImageDrawable.BitmapImage`。
2. 清除 `Document.OverlayerModel` 中已有的所有本地 `RectROIDrawable`。
3. 执行 `Document.View.ZoomToFit()`。

这样不会把上一张图片的 ROI 带到新图片上。

### 10.2 ROI Count

Demo 使用文本框绑定 `ROICount`：

```csharp
[ObservableProperty]
public partial int ROICount { get; set; } = 4;
```

当 `ROICount <= 0` 时不生成 ROI。

假设图片宽度为 `W`，矩形数量为 `N`：

```text
ROI 宽度 = W / (2N - 1)
ROI 间距 = ROI 宽度
第 i 个 ROI 的 X = 图片 Point.X + i × 2 × ROI 宽度
```

因此 N 个矩形和 N-1 个间隔正好覆盖图片宽度，矩形之间的间距等于矩形宽度。

矩形高度为图片高度的 50%。当前画布使用笛卡尔坐标，图片顶部对应 `Point.Y + imageHeight`，所以 ROI 的 Y 起点为：

```csharp
var roiY = BitmapImageDrawable.Point.Y + imageHeight - roiHeight;
```

这会使矩形靠近图片顶部，而不是图片底部。

### 10.3 三个 Demo 命令

#### All Anchors + Move

命令：`EditAllAnchorROIsCommand`

```csharp
ControlPointTypesEnum = RectROIDrawableControlPointTypesEnum.All;
IsEnableDragMove = true;
```

生成数量由 `ROICount` 决定，所有矩形显示八个锚点并支持本体移动。

#### Bottom Anchor Only + No Move

命令：`EditBottomAnchorROICommand`

```csharp
ControlPointTypesEnum = RectROIDrawableControlPointTypesEnum.XCenterYMin;
IsEnableDragMove = false;
```

每个矩形只显示下边中点锚点，不允许拖拽 ROI 本体移动，但可以通过该锚点缩放高度。

#### Three Bottom Anchors + Move

命令：`EditThreeBottomAnchorROIsCommand`

```csharp
ControlPointTypesEnum =
    RectROIDrawableControlPointTypesEnum.XMinYMin |
    RectROIDrawableControlPointTypesEnum.XCenterYMin |
    RectROIDrawableControlPointTypesEnum.XMaxYMin;

IsEnableDragMove = true;
```

每个矩形显示底部左角、底部中点和底部右角三个锚点，并支持本体移动。

三个命令共用 `EditROIsAsync`，只通过锚点 Flags 和 `IsEnableDragMove` 参数区分行为，避免 Demo 代码重复。

## 11. StageMap ProcessStage2Residuals 报错定位

错误信息：

```text
residuals 的形状必须为 (扫描次数, 网格..., 2)
```

### 11.1 当前调用流程

`Step3Async` 中的流程是：

```csharp
Cache.RepeatStageMaps = [];

var historyStageMaps = Cache.RepeatStageMaps;
Cache.RepeatStageMaps = [.. historyStageMaps, scanStageMap];

isSuccess = ProcessStage2Residuals(historyStageMaps, scanStageMap);
```

第一次循环时：

```text
historyStageMaps.Length == 0
```

虽然当前扫描结果已经加入了 `Cache.RepeatStageMaps`，但调用 `ProcessStage2Residuals` 时传入的仍然是加入当前扫描前的空数组。

因此 `ProcessStage2Residuals` 生成的 `pyResiduals` 和 `pyMasks` 都为空：

```csharp
using var pyResiduals = new PyList();
using var pyMasks = new PyList();
foreach (var stageMap in historyStageMaps)
{
    pyResiduals.Append(stageMap.ToPythonErrorMatrix());
    pyMasks.Append(stageMap.ToPythonIsMatchMatrix());
}
```

Python 中执行：

```python
values = np.asarray(residuals, dtype=float)
```

空列表的形状是 `(0,)`，不满足 Python 函数要求的：

```text
(扫描次数, 网格..., 2)
```

所以触发形状异常。

### 11.2 正确的数据要求

Python `process_stage2_residuals` 要求：

```text
residuals.shape == (M, 网格维度..., 2)
masks.shape    == (M, 网格维度...)
```

其中 `M` 是已经完成的扫描次数，当前扫描也必须包含在历史中，并且 `residuals[i]` 与 `masks[i]` 必须一一对应。

调用方需要传入包含当前 `scanStageMap` 的完整历史，而不是只传旧的 `historyStageMaps`。

### 11.3 修复时还需要处理的第二个问题

Python 在累计扫描次数小于 `m_min` 时会返回：

```python
(True, None)
```

当前 C# 代码无条件执行：

```csharp
using var pyResidualTable = Guard.IsNotNullAndReturn(result[1]);
scanStageMap.ApplyPythonErrorMatrix(pyResidualTable);
```

因此修复空数组问题后，前几次扫描还需要正确处理 `residual_table == None`：

- `need_more_measurement == true` 时继续扫描。
- `residual_table == None` 时不要调用 `ApplyPythonErrorMatrix`。
- 只有返回有效残差表时才更新 `scanStageMap`。

`m_max` 还需要和业务定义的最大扫描次数保持一致。当前代码使用 `Cache.StageMapRepeatTimes + 1`，而循环实际扫描次数从 1 到 `Cache.StageMapRepeatTimes`，修复时应确认“RepeatTimes”表示重复扫描次数还是包含初始扫描的总次数。

## 12. 验证清单

### ROI 编辑器

- [ ] 图片为空时编辑器不进入编辑循环。
- [ ] 默认锚点数量为 8。
- [ ] `None` 不显示锚点。
- [ ] 单个 Flags 只显示对应锚点。
- [ ] `All` 显示 8 个锚点。
- [ ] `Rect` 改变后 `IsModified == true`。
- [ ] 普通点选只保留一个 ROI。
- [ ] Ctrl 点选可以追加和取消选择。
- [ ] 框选按相交规则选择。
- [ ] 正向和反向框选结果一致。
- [ ] 多选移动保持相对位置。
- [ ] 多选缩放使用相同锚点和拖动向量。
- [ ] 所有移动和缩放结果不超出图片范围。
- [ ] Enter 保留修改。
- [ ] Escape、取消、超时和外部取消恢复原始状态。

### Demo

- [ ] 打开新图片后旧 ROI 被清空。
- [ ] `ROICount = N` 生成 N 个矩形。
- [ ] 矩形之间的间距等于 ROI 宽度。
- [ ] ROI 顶部贴近图片顶部。
- [ ] 三个 Demo 命令的锚点和移动开关符合按钮文本。

### StageMap

- [ ] 首次调用传入至少一个包含当前扫描的残差。
- [ ] `residual_table == None` 时不更新残差矩阵。
- [ ] `residuals` 和 `masks` 历史长度一致。
- [ ] 实际扫描次数不超过 `m_max`。
- [ ] 达到 `m_min` 后才应用残差表。
