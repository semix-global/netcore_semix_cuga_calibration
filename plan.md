# CIB Aging (老化) 功能计划

## 1. 目标

创建一个CIB老化检测工具窗口，用于验证CIBMMD校准数据是否发生老化衰减。

## 2. 核心功能

1. 在CIBMMD校准Verify时，自动收集原始校准数据到老化字典
2. 老化工具窗口加载CIBMMDCache的参数
3. 支持选择已有的老化数据项进行老化测试
4. 对每个选中项：
   - 二分查找合适的Coefficient，使实际功率等于原始MeasurePower
   - 使用该Coefficient遍历所有Gain，采集PMT图像，得到新的PMT Value曲线
   - 对比原始曲线和新曲线，判断是否老化衰减
5. 随机抽样对比，按设定比例阈值判断是否OK

## 3. 文件清单

### 新建文件

| # | 文件路径 | 说明 |
|---|---------|------|
| 1 | `Sources/Cores/Core.Models/Models/CIB/MMD/CIBAgingItem.cs` | 老化数据项DTO |
| 2 | `Sources/Cuga-Calibration-Solution/ViewModels/Common/Windows/Tools/CIB/CIBAgingCache.cs` | 老化Cache类 |
| 3 | `Sources/Cuga-Calibration-Solution/ViewModels/Common/Windows/Tools/CIB/CIBAgingData.cs` | 老化数据库持久化DTO |
| 3 | `Sources/Cuga-Calibration-Solution/ViewModels/Common/Windows/Tools/CIB/CIBAgingWindowViewModel.cs` | 老化窗口ViewModel |
| 4 | `Sources/Cuga-Calibration-Solution/Views/Common/Windows/Tools/CIB/CIBAgingWindow.xaml` | 老化窗口XAML |
| 5 | `Sources/Cuga-Calibration-Solution/Views/Common/Windows/Tools/CIB/CIBAgingWindow.xaml.cs` | 老化窗口代码后台 |

### 修改文件

| # | 文件路径 | 说明 |
|---|---------|------|
| 1 | `Sources/Cuga-Calibration-Solution/ViewModels/CIB/CIBMMDViewModel.cs` | Verify时通过cacheProvider存储老化数据 |
| 2 | `Sources/Cores/Core.Models/Models/Common/Cookies/ApplicationCookie.cs` | 移除CIBAgingDictionary |
| 3 | `Sources/Cuga-Calibration-Solution/Cuga-Calibration-Solution.csproj` | 添加新文件到项目 |

## 4. 类设计

### CIBAgingItem
- `CIBInformation`: 标识
- `Items`: 原始CIBMMD校准数据（CIBMMDDTOItem列表）
- `NewItems`: 老化测试采集的新数据（CIBMMDDTOItem列表）
- `ScatterPlotControl`: 绘图控件（2个图）
- `IsCalibrated`/`IsVerified`: 状态标记

### CIBAgingData
- 继承`ObservableCacheBase`（实现`ICacheItem`，支持cacheProvider数据库存储）
- `Items`: `CIBAgingItem`列表，通过`cacheProvider.GetOrDefault<CIBAgingData>()`读取/写入

### CIBAgingCache
- 从CIBMMDCache复制的参数（用于界面显示和算法）
- `AgingItems`: 老化数据字典转换的列表
- `SelectedAgingItems`: 选中的老化项
- `CoefficientStep`: 系数步长，默认0.1
- `CoefficientMaxIterations`: 最大迭代次数，默认7
- `CoefficientTolerance`: 功率容差比例，默认0.1
- `SampleCount`: 随机抽样数量
- `AgingThreshold`: 老化比例阈值，默认0.1（10%）

### CIBAgingWindowViewModel
- 继承`ViewModelBase`
- `Cache`: CIBAgingCache
- `Steps`: ["Step 1 Select", "Step 2 Aging Test", "Step 3 Compare"]
- `Step0Async`: 加载参数和老化数据
- `Step1Async`: 执行老化测试（二分查找Coefficient + Gain遍历）
- `Step2Async`: 对比分析并输出结果

## 5. 算法流程

### Step 1: 加载
1. 从CIBMMDCache加载所有参数到Cache
2. 从cacheProvider数据库通过`GetOrDefault<CIBAgingData>()`读取老化数据
3. 过滤：所有Items数量必须相等且MeasurePower相同
4. 显示到界面列表，支持多选

### Step 2: 老化测试（对每个选中项）

#### 2.1 二分查找Coefficient
```
targetPower = item.Items[0].MeasurePower
tolerance = targetPower * Cache.CoefficientTolerance

low = 0, high = 1, step = Cache.CoefficientStep
for iteration in 0..maxIterations:
    coefficient = (low + high) / 2
    下发coefficient，测量实际功率actualPower
    
    if |actualPower - targetPower| <= tolerance:
        found = true, break
    else if actualPower < targetPower:
        low = coefficient
    else:
        high = coefficient
    
    if iteration > 0 and step需要调整:
        step /= 2

if not found: 报错，标记该项失败
```

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

### Step 3: 对比分析
```
对每个选中项：
    从NewItems中按SampleCount随机抽样（均分Gain范围）
    对每个抽样点：
        oldValue = Items中对应Gain的PMTValue
        newValue = NewItems中对应Gain的PMTValue
        if oldValue == 0: continue
        decayRate = |newValue - oldValue| / oldValue
        if decayRate > AgingThreshold:
            标记为老化，记录
    
    if 所有抽样点都未超过阈值:
        IsVerified = true
    else:
        IsVerified = false
```

## 6. 界面设计

参考OpticsBestFocusWindow.xaml：
- 左侧：参数面板（CIBMMDCache参数 + 老化专用参数）
- 中间：选中列表
- 右侧：TabControl显示每个CIB的对比图（原始 vs 新数据）

## 7. 日志

参考BestFocus的HTML日志记录方式：
- `Logger.LogHtmlInformation` 记录参数
- `Logger.LogHtmlHeaderIsOk` / `Logger.LogHtmlHeaderIsError` 记录结果
- 使用`HtmlBullet`, `HtmlTable`, `HtmlContainer`等格式化数据
