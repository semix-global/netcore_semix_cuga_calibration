# CIB MMD 老化检测工具

## 1. 老化检测简介

CIB (Confocal Image Brightfield / Mixed Mode Detection) 是光学检测系统中的核心成像器件。随着使用时间增长，CIB 的 PMT (Photomultiplier Tube) 响应特性会发生衰减，导致测量信号强度下降。为了及时发现这种老化现象，避免影响检测精度，需要定期对 CIB 进行老化检测。

老化检测的基本原理是：
1. **记录基准数据**：在 CIBMMD 校准时，记录各 CIB 在不同 Gain 下的 PMT 响应曲线
2. **重新测量**：经过一段时间后，使用相同的测量条件重新扫描各 CIB 的 PMT 响应
3. **对比分析**：将新的测量数据与原始基准数据进行对比，计算衰减率 (Decay Rate)
4. **判定结果**：如果衰减率超出预设阈值，则判定该 CIB 已老化，需要更换或重新校准

## 2. 数据流

整个老化检测的数据流分为三个阶段：

```
阶段1: CIBMMD 校准
    CIBMMDViewModel 完成校准后，将原始数据保存到 CIBAgingResult
    - CIBAgingResult.Items[].CIBInformation: CIB 标识信息
    - CIBAgingResult.Items[].Items: 原始 CIBMMDDTOItem 列表（各 coefficient 下的 PMT 数据）

阶段2: 老化测试 (Action)
    CIBAgingWindowViewModel.ActionAsync 执行重新测量：
    1. 根据用户选择的 Agings，为每个 CIB 生成 NewItems 容器
    2. 对每个选中的 coefficient，通过调整 AOD Prescan coefficient 使输出功率逼近目标值
    3. 扫描各 Gain 下的 PMT 响应，填充 NewItems
    4. 实时显示系数查找过程和 PMT 曲线

阶段3: 算法分析 (Algorithm)
    CIBAgingAlgorithm.ComputeSampleItems 对比原始数据与重新测量数据：
    1. 对每一对 coefficient（原始 vs 新测量），找出有效的数据点索引
    2. 使用 SampleEvenly 分层随机采样，从中选取固定数量的样本点
    3. 计算每个样本点的衰减率 Decay Rate = (NewPMT - OldPMT) / OldPMT
    4. 判定每个样本点是否通过（|DecayRate| <= AgingThreshold）
    5. 汇总结果，判定每个 CIB 是否整体通过
```

## 3. 核心类说明

### 3.1 CIBAgingCache

缓存配置参数类，保存老化测试的各种配置：

| 属性 | 说明 | 默认值 |
|------|------|--------|
| CIBMMDCache | CIBMMD 校准参数复用 | new() |
| CoefficientStep | 系数查找步长 | 0.1 |
| FindCoefficientRetryTimes | 系数查找最大重试次数 | 7 |
| MeasurePowerRatioThreshold | 功率匹配比率阈值 | 0.1 |
| SampleCount | 采样点数 | 10 |
| AgingThreshold | 衰减率判定阈值 | 0.1 |
| Agings | 可选的 coefficient/measurePower 列表 | [] |
| SelectedAgings | 用户选中的 Agings | [] |
| CoefficientFindItems | 系数查找过程的实时数据 | [] |

### 3.2 CIBAgingResult / CIBAgingItem

结果数据容器：

- **CIBAgingResult**: 顶层容器，包含 `Items` 列表（每个 CIB 一个）
- **CIBAgingItem**: 单个 CIB 的老化数据
  - `CIBInformation`: CIB 标识
  - `Items`: 原始校准数据（所有 coefficient）
  - `SelectItems`: 用户选中的 coefficient 对应的原始数据
  - `NewItems`: 重新测量的数据
  - `SampleItems`: 算法分析后的采样对比结果
  - `IsOk`: 该 CIB 是否通过老化检测
  - `ScatterPlotControl`: ScottPlot 图表控件（懒加载）

### 3.3 CIBAgingSampleItem

每个 coefficient 下的采样分析结果：

| 属性 | 说明 |
|------|------|
| Coefficient | 对应系数 |
| MeasurePower | 对应测量功率 |
| Items | 采样点详情列表 |
| IsOk | 该 coefficient 下所有采样点是否通过 |

其中 `CIBAgingSampleItem.Item` 包含：
- `Index`: 数据点在 Gain 序列中的索引
- `OldPMTValue`: 原始 PMT 值
- `NewPMTValue`: 重新测量的 PMT 值
- `DecayRate`: 衰减率
- `IsOk`: 该采样点是否通过

### 3.4 CIBAgingWindowViewModel

业务逻辑编排器：
- `LoadedAsync`: 从 cacheProvider 加载缓存数据
- `ActionAsync`: 执行完整的老化测试流程（系数查找 + PMT 扫描）
- `AlgorithmAsync`: 对选中的结果项执行算法分析
- `Algorithm`: 调用 `CIBAgingAlgorithm.ComputeSampleItems` 并输出 HTML 日志

### 3.5 CIBAgingAlgorithm

静态工具类，包含纯计算逻辑：
- `SampleEvenly<T>`: 分层随机采样算法
- `ComputeSampleItems`: 衰减率计算核心算法

## 4. 算法说明

### 4.1 系数查找

由于激光器输出功率会随时间变化，直接复用原始 coefficient 可能导致实际功率与目标功率不符。因此老化测试时需要重新查找 coefficient：

1. 从原始 coefficient 开始
2. 测量当前输出功率
3. 计算与目标功率的差值和比率
4. 如果 `|measurePowerRatio| <= MeasurePowerRatioThreshold`，查找成功
5. 否则，根据功率偏差调整 coefficient（过大则减小，过小则增大）
6. 如果前后两次差值符号相反，将步长减半（二分逼近）
7. 重试次数超过 `FindCoefficientRetryTimes` 则报错

### 4.2 分层随机采样 (SampleEvenly)

为了避免采样点集中在某个区域，采用分层随机采样策略：

1. 将 `N` 个有效数据点等分为 `sampleCount` 个 bin
2. 每个 bin 的宽度为 `N / sampleCount`
3. 从每个 bin 中随机选取一个数据点
4. 如果 `N <= sampleCount`，直接返回全部数据点

这样可以保证采样点在 Gain 范围内均匀分布，避免因随机性导致采样偏向某个区间。

### 4.3 衰减率计算

对每个采样点：

```
DecayRate = (NewPMTValue - OldPMTValue) / OldPMTValue
```

特殊情况：
- 如果 `OldPMTValue == 0`，`DecayRate = +Infinity`（必定不通过）

判定规则：
```
IsOk = |DecayRate| <= AgingThreshold
```

整体判定：
- 单个 coefficient：`IsOk = 所有采样点都通过`
- 单个 CIB：`IsOk = 所有 coefficient 都通过 && 至少有一个有效 coefficient`

## 5. UI 设计与操作

### 5.1 界面布局

老化检测窗口采用左右分栏布局：

**左侧面板（参数与操作）**：
- **CIB MMD Params**: CIBMMD 校准参数（显微镜镜头、生产力信息、AOD 波形参数、MMD 参数）
- **Aging Params**: 老化专用参数
  - Coefficient Step: 系数查找步长
  - Find Coefficient Retry Times: 最大重试次数
  - Measure Power Ratio Threshold: 功率匹配阈值
  - Sample Count: 采样点数
  - Aging Threshold: 衰减率判定阈值
  - Agings: 可选 coefficient/measurePower 列表（多选）
- **Actions**: 操作按钮
  - Action / Cancel: 执行完整老化测试
  - Algorithm / Cancel: 仅执行算法分析（用于重新计算）
  - 弹出按钮：查看 Prescan/Chirp AOD 波形和系数查找图表

**右侧面板（结果展示）**：
- **上方 ListView**: 显示所有 CIB 的结果项
  - 列：CIB Information、Original Items、SelectItems Items、IsOk
  - 绿色 = 通过，红色 = 失败
- **下方 TabControl**: 显示选中 CIB 的详细结果
  - **Plot**: ScottPlot 图表，展示原始曲线和重新测量曲线的对比
  - **Sample Data**: 采样分析结果表格
    - 每行对应一个 coefficient
    - 嵌套 DataGrid 显示每个采样点的 Index、Old PMT、New PMT、Decay Rate
    - 绿色 = 通过，红色 = 失败

### 5.2 操作流程

1. **准备阶段**：
   - 确保已完成 CIBMMD 校准（数据已保存到 CIBAgingResult）
   - 打开 CIB Aging 窗口

2. **参数配置**：
   - 检查 CIB MMD Params 是否正确（通常从上次校准自动加载）
   - 调整 Aging Params（如需要）
   - 在 Agings 列表中选中需要测试的 coefficient（可多选）

3. **执行测试**：
   - 点击 **Action** 按钮
   - 系统将自动执行：系数查找 → PMT 扫描 → 算法分析
   - 过程中可以查看 Coefficient Find Plots 弹窗观察系数查找过程

4. **查看结果**：
   - 上方 ListView 中绿色行表示通过，红色行表示失败
   - 选中某行，下方 TabControl 显示该 CIB 的详细图表和采样数据
   - 在 Sample Data 中可以看到每个 coefficient 下各采样点的具体衰减率

5. **重新分析**：
   - 如果只需要调整 Aging Threshold 重新判定，可以修改阈值后点击 **Algorithm** 按钮
   - 系统会对选中的结果项重新执行算法分析，无需重新测量

6. **保存与关闭**：
   - 点击关闭按钮时，Cache 和 Result 会自动保存到 cacheProvider
