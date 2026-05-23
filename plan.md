# CIB Aging (老化) 功能计划

## 1. 目标

创建一个CIB老化检测工具窗口，用于验证CIBMMD校准数据是否发生老化衰减。

## 2. 核心功能

1. 在CIBMMD校准Step2Async和VerifyAsync时，自动收集原始校准数据到老化结果
2. 老化工具窗口加载CIBMMDCache的参数和CIBAgingResult数据
3. 支持选择已有的老化系数项进行老化测试
4. 对每个选中的系数项：
   - 自适应步长查找合适的Coefficient，使实际功率等于原始MeasurePower
   - 使用该Coefficient遍历所有Gain，采集PMT图像，得到新的PMT Value曲线
   - 对比原始曲线和新曲线，判断是否老化衰减
5. 随机抽样对比（均分Gain范围），按设定比例阈值判断是否OK

## 3. 文件清单

### 新建文件

| # | 文件路径 | 说明 |
|---|---------|------|
| 1 | `Sources/Cores/Core.Models/Models/CIB/MMD/CIBAgingItem.cs` | 老化数据项DTO（已合并到CIBAgingResult.cs） |
| 2 | `Sources/Cuga-Calibration-Solution/ViewModels/Common/Windows/Tools/CIB/CIBAgingCache.cs` | 老化Cache类 |
| 3 | `Sources/Cuga-Calibration-Solution/ViewModels/Common/Windows/Tools/CIB/CIBAgingResult.cs` | 老化结果数据库持久化DTO |
| 4 | `Sources/Cuga-Calibration-Solution/ViewModels/Common/Windows/Tools/CIB/CIBAgingWindowViewModel.cs` | 老化窗口ViewModel |
| 5 | `Sources/Cuga-Calibration-Solution/Views/Common/Windows/Tools/CIB/CIBAgingWindow.xaml` | 老化窗口XAML |
| 6 | `Sources/Cuga-Calibration-Solution/Views/Common/Windows/Tools/CIB/CIBAgingWindow.xaml.cs` | 老化窗口代码后台 |

### 修改文件

| # | 文件路径 | 说明 |
|---|---------|------|
| 1 | `Sources/Cuga-Calibration-Solution/ViewModels/CIB/CIBMMDViewModel.cs` | Step2Async和VerifyAsync中通过cacheProvider存储老化数据 |

## 4. 类设计

### CIBAgingSampleItem
- `CIBInformation`: 标识
- `Coefficient`: 系数
- `Gain`: 增益
- `OldPMTValue`: 原始PMT值
- `NewPMTValue`: 新采集PMT值
- `DecayRate`: 衰减率
- `IsOk`: 是否通过阈值判断

### CIBAgingItem
- `CIBInformation`: 标识
- `Items`: 原始CIBMMD校准数据（CIBMMDDTOItem列表）
- `SelectItems`: 选中的老化系数项对应的原始数据
- `NewItems`: 老化测试采集的新数据（CIBMMDDTOItem列表）
- `SampleItems`: 抽样对比结果（CIBAgingSampleItem列表）
- `ScatterPlotControl`: 绘图控件（SelectItems vs NewItems对比图）
- `IsOk`: 老化测试是否通过

### CIBAgingResult
- 继承`ObservableCacheBase`（实现`ICacheItem`，支持cacheProvider数据库存储）
- `Items`: `CIBAgingItem`列表，通过`cacheProvider.GetOrDefault<CIBAgingResult>()`读取/写入

### CIBAgingCoefficientFindItem
- `SelectItem`: 选中的老化系数项
- `ScatterPlotControl`: 系数查找过程绘图控件
- `Points`: 搜索过程中的(Coefficient, MeasurePower)点列表
- `TargetMeasurePower`: 目标功率
- `UpperBound`/`LowerBound`: 功率容差上下界
- `AnswerPoint`: 最终找到的系数和功率

### CIBAgingCache
- 从CIBMMDCache复制的参数（用于界面显示和算法）
- `CIBMMDCache`: CIBMMD缓存参数
- `CoefficientStep`: 系数步长，默认0.1
- `FindCoefficientRetryTimes`: 最大迭代次数，默认7
- `MeasurePowerRatioThreshold`: 功率容差比例，默认0.1
- `SampleCount`: 随机抽样数量，默认10
- `AgingThreshold`: 老化比例阈值，默认0.1（10%）
- `Agings`: 老化系数项列表
- `SelectedAgings`: 选中的老化系数项
- `CoefficientFindItems`: 系数查找绘图项列表

### CIBAgingWindowViewModel
- 继承`ViewModelBase`
- `Cache`: CIBAgingCache
- `Result`: CIBAgingResult
- `ActionAsync`: 执行老化测试（自适应步长查找Coefficient + Gain遍历）
- `AlgorithmAsync`: 执行抽样对比算法
- `Algorithm(CIBAgingItem)`: 对单个CIB进行均分Gain范围随机抽样，计算衰减率

## 5. 算法流程

### Step 1: 加载
1. 从CIBMMDCache加载所有参数到Cache
2. 从cacheProvider数据库通过`GetOrDefault<CIBAgingResult>()`读取老化数据
3. 过滤：所有Items数量必须相等且MeasurePower相同
4. 显示到界面列表，支持多选

### Step 2: 老化测试（对每个选中项）

#### 2.1 自适应步长查找Coefficient
```
targetPower = item.MeasurePower
tolerance = targetPower * Cache.MeasurePowerRatioThreshold

currentCoefficient = item.Coefficient
step = Cache.CoefficientStep
prevDiff = null
for times in 0..Cache.FindCoefficientRetryTimes:
    下发currentCoefficient，测量实际功率currentMeasurePower
    diff = currentMeasurePower - targetPower
    measurePowerRatio = |diff| / targetPower
    
    if measurePowerRatio <= tolerance:
        found = true, break
    
    if prevDiff and sign(diff) != sign(prevDiff):
        step /= 2
    
    if currentMeasurePower > targetPower:
        currentCoefficient -= step
    else:
        currentCoefficient += step
    
    prevDiff = diff

if not found: 报错，标记该项失败
```

查找过程中实时更新CoefficientFindItem的Points列表，成功时设置AnswerPoint。

#### 2.2 Gain遍历采集
```
使用找到的Coefficient
按照CIBMMD Step2Async的逻辑：
- 设置AOD波形
- 遍历Gain范围
- 对每个Gain采集PMT图像
- 计算平均PMTValue
- 写入NewItems
```

### Step 3: 抽样对比算法
```
对每个CIBAgingItem：
    对每个系数项（SelectItems和NewItems对应索引）：
        收集所有有效的(Gain, OldPMTValue, NewPMTValue)对
        按SampleCount均分Gain范围随机抽样
        
        对每个抽样点：
            if oldValue == 0: continue
            decayRate = |newValue - oldValue| / oldValue
            if decayRate > AgingThreshold:
                标记为老化
        
    if 所有抽样点都未超过阈值:
        IsOk = true
    else:
        IsOk = false
```

## 6. CIBMMDViewModel集成

### Step2Async
在`await Task.WhenAll(Calibratings.Select(... Algorithm(t)))`之前：
- 获取CIBAgingResult
- 比较Calibratings的CIBInformations和agingData.Items的CIBInformations
- 如果不相等，替换全部agingData.Items为当前Calibratings数据

### VerifyAsync
在`Guard.IsTrue(Save(SelectedReviewItems, cancellationToken))`之前：
- 获取CIBAgingResult
- 比较SelectedReviewItems的CIBInformations和agingData.Items的CIBInformations
- 如果不相等，替换全部agingData.Items为当前SelectedReviewItems的已验证数据

## 7. 界面设计

参考CIBAgingWindow.xaml：
- 左侧：参数面板（CIB MMD Cache参数 + 老化专用参数 + 操作按钮）
- 中间：选中列表
- 右侧：TabControl显示每个CIB的对比图（原始 vs 新数据）和抽样对比结果列表（绿/红）
- Popup：系数查找过程曲线图（Target/Upper/Lower/Answer）

## 8. 日志

参考BestFocus的HTML日志记录方式：
- `Logger.LogHtmlInformation` 记录参数
- `Logger.LogHtmlHeaderIsOk` / `Logger.LogHtmlHeaderIsError` 记录结果
- 使用`HtmlBullet`, `HtmlTable`, `HtmlContainer`等格式化数据
