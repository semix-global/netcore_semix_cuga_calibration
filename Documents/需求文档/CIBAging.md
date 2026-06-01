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
    CIBAgingWindowViewModel.Algorithm 对比原始数据与重新测量数据：
    1. 对每一对 coefficient（原始 vs 新测量），找出有效的数据点索引
    2. 使用 SampleEvenly 分层随机采样，从中选取固定数量的样本点
    3. 计算每个样本点的衰减率 DecayRatio = (NewPMT - OldPMT) / OldPMT
    4. 判定每个样本点是否通过（decayRatio > 0 或 |DecayRatio| <= AgingRatioThreshold）
    5. 汇总结果，判定每个 CIB 是否整体通过
```

## 3. 核心类说明

### 3.1 CIBAgingCache

缓存配置参数类，保存老化测试的各种配置：

| 属性 | 类型 | 说明 | 默认值 |
|------|------|------|--------|
| CIBMMDCache | CIBMMDCache | CIBMMD 校准参数复用 | new() |
| CoefficientStep | double | 系数查找步长 | 0.1 |
| FindCoefficientRetryTimes | int | 系数查找最大重试次数 | 7 |
| MeasurePowerRatioThreshold | double | 功率匹配比率阈值 | 0.1 |
| AgingPMTValueNoises | double | PMT 噪声阈值，低于此值的 PMT 数据点不参与老化计算 | 20 |
| AgingSampleCount | int | 采样点数 | 10 |
| AgingRatioThreshold | double | 衰减率判定阈值 | 0.1 |
| Agings | IReadOnlyList&lt;CIBAgingSelectItem&gt; | 可选的 coefficient/measurePower 列表（从 CIBMMDCache.NotUseODFilterMeasurePowerPoints 自动加载） | [] |
| SelectedAgings | IReadOnlyList&lt;CIBAgingSelectItem&gt; | 用户选中的 Agings | [] |
| CoefficientFindItems | IReadOnlyList&lt;CIBAgingCoefficientFindItem&gt; | 系数查找过程的实时数据与图表 | [] |

### 3.2 CIBAgingResult / CIBAgingItem

结果数据容器：

- **CIBAgingResult**: 顶层容器
  - `IsModify`: 是否被修改（关闭时判断是否需要保存）
  - `Items`: CIBAgingItem 列表（每个 CIB 一个）

- **CIBAgingItem**: 单个 CIB 的老化数据
  - `CIBInformation`: CIB 标识
  - `Items`: 原始校准数据（所有 coefficient）
  - `SelectItems`: 用户选中的 coefficient 对应的原始数据
  - `NewItems`: 重新测量的数据
  - `SampleItems`: 算法分析后的采样对比结果
  - `IsOk`: 该 CIB 是否通过老化检测
  - `ScatterPlotControl`: ScottPlot 图表控件

### 3.3 CIBAgingSampleItem

每个 coefficient 下的采样分析结果：

| 属性 | 类型 | 说明 |
|------|------|------|
| Coefficient | double | 对应系数 |
| MeasurePower | double | 对应测量功率 |
| Items | IReadOnlyList&lt;Item&gt; | 采样点详情列表 |
| IsOk | bool | 该 coefficient 下所有采样点是否通过 |

其中 `CIBAgingSampleItem.Item` 包含：
- `Gain`: 数据点对应的 Gain 值
- `OldPMTValue`: 原始 PMT 值
- `NewPMTValue`: 重新测量的 PMT 值
- `DecayRatio`: 衰减率
- `IsOk`: 该采样点是否通过

### 3.4 CIBAgingCoefficientFindItem

系数查找过程的实时数据与可视化：

| 属性 | 类型 | 说明 |
|------|------|------|
| SelectItem | CIBAgingSelectItem | 当前查找的目标 coefficient/measurePower |
| FindMeasurePowerPoints | IReadOnlyList&lt;Point&gt; | 查找过程中记录的 (coefficient, measurePower) 点序列 |
| TargetMeasurePower | double | 目标功率 |
| UpperMeasurePower | double | 功率上限 = TargetMeasurePower * (1 + MeasurePowerRatioThreshold) |
| LowerMeasurePower | double | 功率下限 = TargetMeasurePower * (1 - MeasurePowerRatioThreshold) |
| AnswerMeasurePowerPoint | Point? | 查找成功后的最终 (coefficient, measurePower) |
| AnswerMeasurePowerRatio | double? | 查找成功后的功率比率偏差 |
| ScatterPlotControl | IScatterPlotControl | 实时显示查找过程的 ScottPlot 图表 |

图表显示内容：
- Y轴：功率(mW)，X轴：coefficient
- 深红色实线：Target Measure Power
- 橙红色虚线：Upper / Lower Measure Power
- 绿色实线：Answer Measure Power / Answer Coefficient
- 散点：Search Measure Power Points

### 3.5 CIBAgingWindowViewModel

业务逻辑编排器：
- `LoadedAsync`: 从 cacheProvider 加载 Cache 和 Result，自动将 CIBMMDCache.NotUseODFilterMeasurePowerPoints 填充为 Agings
- `ActionAsync`: 执行完整的老化测试流程（系数查找 + PMT 扫描 + 算法分析）
- `AlgorithmAsync`: 仅对 `SelectedResultItems` 执行算法分析（用于重新计算，无需重新测量）
- `Algorithm`: 核心算法方法，计算衰减率并输出 HTML 日志
- `Close`: 保存 Cache 和 Result 到 cacheProvider

## 4. 参数详解

### 4.1 CIB MMD 参数

老化测试复用 CIBMMD 校准参数，确保测量条件与原始校准一致：

| 参数组 | 说明 |
|--------|------|
| Default | 显微镜镜头、生产力信息、光学配置、Stage 位置 |
| Prescan AOD Waveform | Prescan 频率、波形生成参数 |
| Chirp AOD Waveform | Chirp 频率、波形生成参数 |
| MMD | 测量功率等待时间、PMT 值等待时间、Start/Step/Stop Gain、保护 PMT 值、保护次数、图像宽度 |

### 4.2 Aging 参数

| 参数 | 说明 | 对测试的影响 |
|------|------|-------------|
| CoefficientStep | 系数查找步长 | 初始调整 coefficient 的步长，过大可能导致振荡，过小则收敛慢 |
| FindCoefficientRetryTimes | 最大重试次数 | 超过此次数未找到合适系数则报错 |
| MeasurePowerRatioThreshold | 功率匹配比率阈值 | 判定系数查找成功的标准：`\|measurePowerRatio\| <= 阈值` |
| AgingPMTValueNoises | PMT 噪声阈值 | PMT 值低于此阈值的数据点不参与老化计算，避免低信噪比数据干扰结果 |
| AgingSampleCount | 采样点数 | 每个 coefficient 下随机采样的数据点数量 |
| AgingRatioThreshold | 衰减率判定阈值 | 判定老化的标准：衰减率（负值）绝对值超过此阈值则判定为老化 |
| Agings | 待测 coefficient 列表 | 用户多选，决定本次测试测量哪些 coefficient |

### 4.3 参数设置建议

- **AgingPMTValueNoises**: 根据实际 PMT 底噪设置，通常设为 20（DC 值），确保低响应区域不参与计算
- **AgingRatioThreshold**: 根据老化判定严格程度设置，默认 0.1（10%）。若要求更宽松可设为 0.2（20%）
- **AgingSampleCount**: 数据点较多时可适当增大，确保采样覆盖不同 Gain 区间；数据点较少时可减小或设为等于数据点数量

## 5. Action 流程详解

`ActionAsync` 是完整的老化测试流程，分为以下步骤：

### 5.1 前置检查

1. 检查用户是否已选择 Agings（`SelectedAgings.Count > 0`）
2. 按 Coefficient 升序排序 SelectedAgings
3. 获取光功率计校准数据（`laserOpticalPowerMeter`），找到 MaxMeasurePowerPosition
4. 获取所有 CIB 标识（`allCibInformations`）

### 5.2 设备初始化

1. 切换显微镜镜头到 CIBMMDCache 配置的镜头
2. 移动 Stage 到激光器最大测量功率位置
3. 生成 Prescan 和 Chirp AOD 波形
4. 设置激光器 AOD 波形（初始 coefficient = CIBMMDCache.StartCoefficient）
5. 设置 CIB 为 PMTVoltage 模式，初始 Gain = StartGain
6. 关闭 OD Filter

### 5.3 结果容器初始化

对每个 CIB 的 CIBAgingItem：
1. 清空 SelectItems、NewItems、SampleItems
2. 将 `IsOk` 设为 false
3. 根据 SelectedAgings，从 `Items` 中克隆匹配的 CIBMMDDTOItem 到 SelectItems
4. 为每个 SelectedAging 创建空的 NewItems（PMTValue 初始为 NaN）

初始化 CoefficientFindItems（每个 SelectedAging 一个）：
- TargetMeasurePower = MeasurePower
- UpperMeasurePower = MeasurePower * (1 + MeasurePowerRatioThreshold)
- LowerMeasurePower = MeasurePower * (1 - MeasurePowerRatioThreshold)

### 5.4 系数查找

对每个 SelectedAging（按 coefficient 升序）：

1. **初始化**
   - currentCoefficient = 原始 coefficient
   - targetMeasurePower = MeasurePower
   - step = CoefficientStep
   - prevDiff = null

2. **迭代查找**（最多 FindCoefficientRetryTimes 次）
   - 设置激光器 Prescan AOD 波形为 currentCoefficient
   - 切换 AOD 为 Through 模式，等待 MeasurePowerWaitTime
   - 测量当前输出功率 currentMeasurePower
   - 恢复 AOD 为 Scan 模式
   - 记录 (currentCoefficient, currentMeasurePower) 到 FindMeasurePowerPoints
   - 计算 diff = currentMeasurePower - targetMeasurePower
   - 计算 measurePowerRatio = diff / targetMeasurePower
   - **成功判定**：如果 `\|measurePowerRatio\| <= MeasurePowerRatioThreshold`
     - 记录 AnswerMeasurePowerPoint 和 AnswerMeasurePowerRatio
     - 跳出循环
   - **调整系数**：
     - 如果 prevDiff 与当前 diff 符号相反，step 减半（二分逼近）
     - 如果 currentMeasurePower > targetMeasurePower，currentCoefficient -= step
     - 否则 currentCoefficient += step
     - 检查 coefficient 范围 [0, 1]，超出则报错
   - prevDiff = diff

3. **失败处理**
   - 如果超过最大重试次数仍未成功，抛出异常
   - 异常会被捕获并记录到 HTML 日志，继续下一个 SelectedAging

### 5.5 PMT 扫描

系数查找成功后，对每个 CIB 扫描各 Gain 下的 PMT 响应：

1. 将查找到的 currentCoefficient 和 currentMeasurePower 写入每个 CIB 的 NewItems[coefficientIndex]
2. 移动 Stage 到 Haze 暗场位置
3. 设置 Gain = StartGain
4. 设置激光器 Prescan AOD 波形为查找到的 currentCoefficient
5. 切换 AOD 为 Through 模式

对每个 Gain（从 StartGain 到 StopGain，步长 StepGain）：
1. 跳过已完成测量的 CIB（PMTValue 不是 NaN）
2. 跳过超过保护次数的 CIB
3. 设置所有未保护 CIB 的 Gain
4. 等待 PMTValueWaitTime
5. 采集各 CIB 的 PMT 图像（Dark Field）
6. 计算图像平均亮度作为 PMTValue
7. **PMT 保护机制**：
   - 如果 PMTValue >= ProtectedPMTValue，增加 ProtectedOverflowProtectedPMTValueCount
   - 如果超过 ProtectedOverflowProtectedPMTValueCount，保存图像并将该 CIB 的 Gain 重置为 StartGain
8. 记录 PMTValue、RawImageFilePath 到 NewItems
9. 最后一次 Gain 扫描后保存图像

扫描完成后：
- 恢复 AOD 为 Scan 模式
- 记录各 CIB 的 PMT 曲线图表到 HTML 日志

### 5.6 算法分析

所有 coefficient 扫描完成后，对每个 CIB 执行 Algorithm：
1. 计算各采样点的衰减率
2. 判定是否老化
3. 输出 HTML 日志和图表

### 5.7 结果显示

弹出对话框显示算法结果：
- 每个 CIB 的判定结果（OK / Already aged）
- 整体结果（OK / Failed）

### 5.8 清理

无论成功或失败，最后都会执行：
- 开启 CIB AGC
- 恢复 CIB 为 PMTLog 模式
- Stage 回到 BrightField 位置

## 6. Algorithm 流程详解

`Algorithm` 是核心分析逻辑，对比原始数据与重新测量数据：

### 6.1 数据过滤

对每一对 coefficient（SelectItems[coefficientIndex] vs NewItems[coefficientIndex]）：
1. 遍历所有数据点索引
2. **有效数据点判定**：
   - `!double.IsNaN(oldPMTValue)`
   - `!double.IsNaN(newPMTValue)`
   - `oldPMTValue >= AgingPMTValueNoises`
   - `newPMTValue >= AgingPMTValueNoises`
3. 如果没有有效数据点，跳过该 coefficient

### 6.2 分层随机采样

使用 `SampleEvenly` 从有效数据点中采样：
1. 将 N 个有效点等分为 `AgingSampleCount` 个 bin
2. 每个 bin 的宽度为 `N / AgingSampleCount`
3. 从每个 bin 中随机选取一个数据点
4. 如果 `N <= AgingSampleCount`，直接返回全部数据点

### 6.3 衰减率计算

对每个采样点：
```
DecayRatio = (NewPMTValue - OldPMTValue) / OldPMTValue
```

特殊情况：
- 如果 `OldPMTValue == 0`，`DecayRatio = +Infinity`（必定不通过）

### 6.4 老化判定

```
IsOk = decayRatio > 0 || Math.Abs(decayRatio) <= AgingRatioThreshold
```

判定逻辑说明：
- **decayRatio > 0**: PMT 响应增强（New > Old），不算老化，直接通过
- **Math.Abs(decayRatio) <= AgingRatioThreshold**: 衰减率在允许范围内，通过
- **否则**: PMT 响应衰减且超过阈值，判定为老化，不通过

示例（AgingRatioThreshold = 0.1）：
| OldPMT | NewPMT | DecayRatio | 判定 |
|--------|--------|-----------|------|
| 100 | 105 | +0.05 | 通过（增强） |
| 100 | 95 | -0.05 | 通过（衰减 5% <= 10%） |
| 100 | 80 | -0.20 | 不通过（衰减 20% > 10%） |
| 100 | 0 | -1.00 | 不通过（完全失效） |

### 6.5 结果汇总

- 单个 coefficient：`IsOk = 所有采样点都通过`
- 单个 CIB：`IsOk = 所有 coefficient 都通过 && 至少有一个有效 coefficient`

### 6.6 HTML 日志输出

对每个 CIB：
- OK：绿色标题，显示 CIBInformation、SampleItems 表格、Plot 图表
- Error：红色标题，显示相同内容

## 7. UI 设计与操作

### 7.1 界面布局

老化检测窗口采用左右分栏布局：

**左侧面板（参数与操作）**：
- **CIB MMD Params**: CIBMMD 校准参数（显微镜镜头、生产力信息、AOD 波形参数、MMD 参数）
- **Aging Params**: 老化专用参数
  - Coefficient Step: 系数查找步长
  - Find Coefficient Retry Times: 最大重试次数
  - Measure Power Ratio Threshold: 功率匹配阈值
  - Aging PMT Value Noises: PMT 噪声阈值
  - Aging Sample Count: 采样点数
  - Aging Ratio Threshold: 衰减率判定阈值
  - Agings: 可选 coefficient/measurePower 列表（多选）
- **Actions**: 操作按钮
  - Action / Cancel: 执行完整老化测试
  - Algorithm / Cancel: 仅执行算法分析（用于重新计算）
  - 弹出按钮：查看 Prescan/Chirp AOD 波形和系数查找图表

**右侧面板（结果展示）**：
- **上方 ListView**: 显示所有 CIB 的结果项
  - 列：CIB Information、Original Items、SelectItems Items、IsOk
  - 绿色 = 通过，红色 = 失败
- **下方 TabControl**: 显示选中 CIB 的详细结果（通过 `SelectedResultItems` 绑定）
  - **Plot**: ScottPlot 图表
    - 浅色线：原始 SelectItems 曲线
    - 深色线：重新测量的 NewItems 曲线（标注 "Aging"）
    - 垂直线：采样点位置
  - **Sample Data**: 采样分析结果表格
    - 每行对应一个 coefficient
    - 嵌套 DataGrid 显示每个采样点的 Gain、Old PMT、New PMT、Decay Rate
    - 绿色 = 通过，红色 = 失败

### 7.2 操作流程

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
   - 选中某行（可多选），下方 TabControl 显示该 CIB 的详细图表和采样数据
   - 在 Sample Data 中可以看到每个 coefficient 下各采样点的具体衰减率

5. **重新分析**：
   - 如果只需要调整 Aging Ratio Threshold 或 Aging PMT Value Noises 重新判定：
     - 修改参数后，在上方 ListView 中选中需要重新分析的行
     - 点击 **Algorithm** 按钮
   - 系统会对选中的结果项重新执行算法分析，无需重新测量

6. **保存与关闭**：
   - 点击关闭按钮时，Cache 和 Result 会自动保存到 cacheProvider

## 8. 注意事项

1. **测量前确保 CIBMMD 校准已完成**：老化检测依赖 CIBMMD 校准保存的原始数据（CIBAgingResult），如果没有数据会弹出警告
2. **系数查找可能失败**：如果激光器功率变化过大或 coefficient 范围 [0, 1] 内无法匹配目标功率，会报错并记录到日志
3. **PMT 保护机制**：如果 PMT 响应超过 ProtectedPMTValue，系统会自动保护该 CIB（重置 Gain），防止损坏
4. **衰减率判定方向性**：系统只判定衰减（PMT 下降）为老化，PMT 增强不算老化。这是基于 PMT 老化通常表现为响应下降的特性
5. **低信噪比数据过滤**：AgingPMTValueNoises 参数过滤掉低 PMT 值数据点，避免噪声干扰判定结果
