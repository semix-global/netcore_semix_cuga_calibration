# 校准文件说明

---

## 1. 校准文件如何获取

> `D:\Nano\Cuga-Calibration\CalibrationResult\Result_日期时间.dat`
> 
> - 按照日期进行分类排序存储的

## 2. 怎么验证校准对象是否可以使用

> 任何校准对象都是继承自`CalibrationBase`
>   Cuga应用时需先读取标志位`CalibrationBase.IsRequiredSelfCheck`判断当前校准是否为Cuga初始化的必要条件项
>   `CalibrationBase.IsRequiredSelfCheck`为true时，使用`CalibrationBase.IsOk`判断改校准对象是否能够使用

```csharp
[Serializable]
public class CalibrationBase
{
    /// <summary>
    /// 是否校准Ok
    /// </summary>
    public bool IsCalibrated { get; set; }

    /// <summary>
    /// 是否验证Ok
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Cuga初始化是否需要自检此项校准结果是否Ok
    /// </summary>
    public bool IsRequiredCalibrate { get; set; } = false;

    /// <summary>
    /// 是否Ok
    /// </summary>
    [JsonIgnore]
    public bool IsOk => IsCalibrated && IsVerified;
}
```

## 3. 有哪些大类

```csharp
/// <summary>
/// 校准对象序列化
/// </summary>
[Serializable]
public sealed class CalibrationObj
{
    /// <summary>
    /// 缓震平台校准对象
    /// </summary>
    [Description(WcfConstantHelper.AdsNodeCalibrationName)]
    public CalibrationAdsObj CalibrationAdsObj { get; set; } = new CalibrationAdsObj();

    /// <summary>
    /// 显微镜校准对象
    /// </summary>
    [Description(WcfConstantHelper.MicroscopeNodeCalibrationName)]
    public CalibrationMicroscopeObj CalibrationMicroscopeObj { get; set; } = new CalibrationMicroscopeObj();

    /// <summary>
    /// Chuck校准对象
    /// </summary>
    [Description(WcfConstantHelper.ChuckNodeCalibrationName)]
    public CalibrationChuckObj CalibrationChuckObj { get; set; } = new CalibrationChuckObj();

    /// <summary>
    /// 激光校准对象
    /// </summary>
    [Description(WcfConstantHelper.LaserNodeCalibrationName)]
    public CalibrationLaserObj CalibrationLaserObj { get; set; } = new CalibrationLaserObj();

    /// <summary>
    /// 暗场自动聚焦校准对象
    /// </summary>
    [Description(WcfConstantHelper.AutoFocusNodeCalibrationName)]
    public CalibrationAutoFocusObj CalibrationAutoFocusObj { get; set; } = new CalibrationAutoFocusObj();
}
```

# 1. 缓震平台: `CalibrationAdsObj`

---

```csharp
/// <summary>
/// 缓震平台校准对象
/// </summary>
[Serializable]
public sealed class CalibrationAdsObj
{
    /// <summary>
    /// 压力前馈校准对象
    /// </summary>
    [Description(WcfConstantHelper.AdsPressureCalibrationName)]
    public CalibrationAdsPressureGains CalibrationAdsPressureGains { get; set; } = new CalibrationAdsPressureGains();

    /// <summary>
    /// X方向速度前馈校准对象列表
    /// </summary>
    [Description(WcfConstantHelper.AdsXGainCalibrationName)]
    public CalibrationAdsXGainsItem CalibrationAdsXGains { get; set; } = new CalibrationAdsXGainsItem();

    /// <summary>
    /// Y方向速度前馈校准对象
    /// </summary>
    [Description(WcfConstantHelper.AdsYGainCalibrationName)]
    public CalibrationAdsYGainsItem CalibrationAdsYGains { get; set; } = new CalibrationAdsYGainsItem();
}
```

## 1.1. 压力前馈校准: `CalibrationAdsPressureGains`

```csharp
/// <summary>
/// 压力前馈校准对象
/// </summary>
[Serializable]
public sealed class CalibrationAdsPressureGains : CalibrationBase
{
    /// <summary>
    /// 比例阀输出值1(绝对), **需要下发ADS硬件**
    /// </summary>
    public double PressureValue1 { get; set; }

    /// <summary>
    /// 比例阀输出值2(绝对), **需要下发ADS硬件**
    /// </summary>
    public double PressureValue2 { get; set; }

    /// <summary>
    /// 比例阀输出值3(绝对), **需要下发ADS硬件**
    /// </summary>
    public double PressureValue3 { get; set; }
}
```

## 1.2. X方向速度前馈校准: `CalibrationAdsXGainsItem`

> - 注意下发的时候的正反向
> - 下发是根据速度二次多项式拟合
>   - 正向 `X1 = PositiveX1P1* v^2 + PositiveX1P2* v^1 + PositiveX1P3`
>   - 正向 `X2 = PositiveX2P1* v^2 + PositiveX2P2* v^1 + PositiveX2P3`
>   - 反向 `X3 = NegativeX3P1* v^2 + NegativeX3P2* v^1 + NegativeX3P3`
>   - 反向 `X4 = NegativeX4P1* v^2 + NegativeX4P2* v^1 + NegativeX4P3`

```csharp
/// <summary>
/// X方向速度前馈校准对象
/// </summary>
[Serializable]
public sealed class CalibrationAdsXGainsItem : CalibrationBase
{
    /// <summary>
    /// X1正速度前馈系数X1的二次多项式二次系数
    /// </summary>
    public double PositiveX1P1 { get; set; }

    /// <summary>
    /// X1正速度前馈系数X1的二次多项式一次系数
    /// </summary>
    public double PositiveX1P2 { get; set; }

    /// <summary>
    /// X1正速度前馈系数X1的二次多项式常数项
    /// </summary>
    public double PositiveX1P3 { get; set; }

    /// <summary>
    /// X2正速度前馈系数X2的二次多项式二次系数
    /// </summary>
    public double PositiveX2P1 { get; set; }

    /// <summary>
    /// X2正速度前馈系数X2的二次多项式一次系数
    /// </summary>
    public double PositiveX2P2 { get; set; }

    /// <summary>
    /// X2正速度前馈系数X2的二次多项式常数项
    /// </summary>
    public double PositiveX2P3 { get; set; }

    /// <summary>
    /// X3负速度前馈系数X3的二次多项式二次系数
    /// </summary>
    public double NegativeX3P1 { get; set; }

    /// <summary>
    /// X3负速度前馈系数X3的二次多项式一次系数
    /// </summary>
    public double NegativeX3P2 { get; set; }

    /// <summary>
    /// X3负速度前馈系数X3的二次多项式常数项
    /// </summary>
    public double NegativeX3P3 { get; set; }

    /// <summary>
    /// X4负速度前馈系数X4的二次多项式二次系数
    /// </summary>
    public double NegativeX4P1 { get; set; }

    /// <summary>
    /// X4负速度前馈系数X4的二次多项式一次系数
    /// </summary>
    public double NegativeX4P2 { get; set; }

    /// <summary>
    /// X4负速度前馈系数X4的二次多项式常数项
    /// </summary>
    public double NegativeX4P3 { get; set; }
}
```

## 1.3. Y方向速度前馈校准: `CalibrationAdsYGainsItem`

> - 注意下发的时候的正反向
> - 下发是根据速度二次多项式拟合
>   - 正向 `Y1 = PositiveY1P1* v^2 + PositiveY1P2* v^1 + PositiveY1P3`
>   - 正向 `Y2 = PositiveY2P1* v^2 + PositiveY2P2* v^1 + PositiveY2P3`
>   - 正向 `Y3 = PositiveY3P1* v^2 + PositiveY3P2* v^1 + PositiveY3P3`
>   - 反向 `Y4 = NegativeY4P1* v^2 + NegativeY4P2* v^1 + NegativeY4P3`
>   - 反向 `Y5 = NegativeY5P1* v^2 + NegativeY5P2* v^1 + NegativeY5P3`
>   - 反向 `Y6 = NegativeY6P1* v^2 + NegativeY6P2* v^1 + NegativeY6P3`

```csharp
/// <summary>
/// Y方向速度前馈校准对象
/// </summary>
[Serializable]
public sealed class CalibrationAdsYGainsItem : CalibrationBase
{
    /// <summary>
    /// Y1正速度前馈系数Y1的二次多项式二次系数
    /// </summary>
    public double PositiveY1P1 { get; set; }

    /// <summary>
    /// Y1正速度前馈系数Y1的二次多项式一次系数
    /// </summary>
    public double PositiveY1P2 { get; set; }

    /// <summary>
    /// Y1正速度前馈系数Y1的二次多项式常数项
    /// </summary>
    public double PositiveY1P3 { get; set; }

    /// <summary>
    /// Y2正速度前馈系数Y2的二次多项式二次系数
    /// </summary>
    public double PositiveY2P1 { get; set; }

    /// <summary>
    /// Y2正速度前馈系数Y2的二次多项式一次系数
    /// </summary>
    public double PositiveY2P2 { get; set; }

    /// <summary>
    /// Y2正速度前馈系数Y2的二次多项式常数项
    /// </summary>
    public double PositiveY2P3 { get; set; }

    /// <summary>
    /// Y3正速度前馈系数Y2的二次多项式二次系数
    /// </summary>
    public double PositiveY3P1 { get; set; }

    /// <summary>
    /// Y3正速度前馈系数Y3的二次多项式一次系数
    /// </summary>
    public double PositiveY3P2 { get; set; }

    /// <summary>
    /// Y3正速度前馈系数Y3的二次多项式常数项
    /// </summary>
    public double PositiveY3P3 { get; set; }

    /// <summary>
    /// Y4负速度前馈系数Y4的二次多项式二次系数
    /// </summary>
    public double NegativeY4P1 { get; set; }

    /// <summary>
    /// Y4负速度前馈系数Y4的二次多项式一次系数
    /// </summary>
    public double NegativeY4P2 { get; set; }

    /// <summary>
    /// Y4负速度前馈系数Y4的二次多项式常数项
    /// </summary>
    public double NegativeY4P3 { get; set; }

    /// <summary>
    /// Y5负速度前馈系数Y5的二次多项式二次系数
    /// </summary>
    public double NegativeY5P1 { get; set; }

    /// <summary>
    /// Y5负速度前馈系数Y5的二次多项式一次系数
    /// </summary>
    public double NegativeY5P2 { get; set; }

    /// <summary>
    /// Y5负速度前馈系数Y5的二次多项式常数项
    /// </summary>
    public double NegativeY5P3 { get; set; }

    /// <summary>
    /// Y6负速度前馈系数Y6的二次多项式二次系数
    /// </summary>
    public double NegativeY6P1 { get; set; }

    /// <summary>
    /// Y6负速度前馈系数Y6的二次多项式一次系数
    /// </summary>
    public double NegativeY6P2 { get; set; }

    /// <summary>
    /// Y6负速度前馈系数Y6的二次多项式常数项
    /// </summary>
    public double NegativeY6P3 { get; set; }
}
```

# 2. 显微镜: `CalibrationMicroscopeObj`

---

```csharp
/// <summary>
/// 显微镜校准对象
/// </summary>
[Serializable]
public sealed class CalibrationMicroscopeObj
{
    /// <summary>
    /// Focus校准对象列表
    /// </summary>
    [Description(WcfConstantHelper.MicroscopeFocusCalibrationName)]
    public CalibrationMicroscopeFocusItem[] CalibrationMicroscopeFocusItemList { get; set; } = Array.Empty<CalibrationMicroscopeFocusItem>();

    /// <summary>
    /// Cal Chip校准对象
    /// </summary>
    [Description(WcfConstantHelper.MicroscopeCalChipCalibrationName)]
    public CalibrationMicroscopeCalChip CalibrationMicroscopeCalChip { get; set; } = new CalibrationMicroscopeCalChip();

    /// <summary>
    /// PixelSize校准对象列表
    /// </summary>
    [Description(WcfConstantHelper.MicroscopePixelSizeCalibrationName)]
    public CalibrationMicroscopePixelSizeItem[] CalibrationMicroscopePixelSizeItemList { get; set; } = Array.Empty<CalibrationMicroscopePixelSizeItem>();

    /// <summary>
    /// Centricity校准对象列表
    /// </summary>
    [Description(WcfConstantHelper.MicroscopeCentricityCalibrationName)]
    public CalibrationMicroscopeCentricityItem[] CalibrationMicroscopeCentricityItemList { get; set; } = Array.Empty<CalibrationMicroscopeCentricityItem>();
}
```

## 2.1. 自动聚焦校准: `CalibrationMicroscopeFocusItem`

> 根据不同 `列表.SingleOrDefault(t => t.CgMicroscopeLens == 镜头)` 判断`is not null`后使用
> 
> 个数：5

```csharp
/// <summary>
/// Focus校准对象
/// </summary>
[Serializable]
public sealed class CalibrationMicroscopeFocusItem : CalibrationBase
{
    /// <summary>
    /// 显微镜镜头
    /// </summary>
    public CgMicroscopeLens CgMicroscopeLens { get; set; }

    /// <summary>
    /// 当前镜头的最佳清晰高度(绝对ECS), **需要下发AF硬件**
    /// </summary>
    public double EcsValue { get; set; }

    /// <summary>
    /// 当前镜头的显微镜相像差镜头电压值(像差镜头绝对位置), **需要下发Microscope硬件**
    /// </summary>
    public double MicroscopeVoltage { get; set; }
}
```

## ==2.2.== Cal Chip4个耳朵自动聚焦校准: `CalibrationMicroscopeCalChip`

```csharp
/// <summary>
/// Cal Chip校准对象
/// </summary>
[Serializable]
public sealed class CalibrationMicroscopeCalChip : CalibrationBase
{
    /// <summary>
    /// 此显微镜镜头下做的校准
    /// </summary>
    public CgMicroscopeLens CgMicroscopeLens { get; set; }

    /// <summary>
    /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, DSW对准角度
    /// </summary>
    public double DSWAlignmentDegree { get; set; }

    /// <summary>
    /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, DSW明场中心根据对准角度放射变化后的机械位置(绝对位置), **需要下发AF硬件**
    /// </summary>
    public CgPoint DswBrightFieldMachinePosition { get; set; }

    /// <summary>
    /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, DSW最佳清晰高度(绝对ECS), **需要下发AF硬件**
    /// </summary>
    public double DswEcsValue { get; set; }

    /// <summary>
    /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, Undefined明场中心的机械位置(绝对位置), **需要下发AF硬件**
    /// </summary>
    public CgPoint UndefinedBrightFieldMachinePosition { get; set; }

    /// <summary>
    /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, Undefined最佳清晰高度(绝对ECS), **需要下发AF硬件**
    /// </summary>
    public double UndefinedEcsValue { get; set; }

    /// <summary>
    /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, Haze明场中心的机械位置(绝对位置), **需要下发AF硬件**
    /// </summary>
    public CgPoint HazeBrightFieldMachinePosition { get; set; }

    /// <summary>
    /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, Haze最佳清晰高度(绝对ECS), **需要下发AF硬件**
    /// </summary>
    public double HazeEcsValue { get; set; }

    /// <summary>
    /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, ShinyWafer明场中心的机械位置(绝对位置), **需要下发AF硬件**
    /// </summary>
    public CgPoint ShinyWaferBrightFieldMachinePosition { get; set; }

    /// <summary>
    /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, ShinyWafer最佳清晰高度(绝对ECS), **需要下发AF硬件**
    /// </summary>
    public double ShinyWaferEcsValue { get; set; }
}
```

## 2.3. 像素尺寸校准: `CalibrationMicroscopePixelSizeItem`

> 根据不同 `列表.SingleOrDefault(t => t.CgMicroscopeLens == 镜头)` 判断`is not null`后使用
> 
> 个数：5

```csharp
/// <summary>
/// PixelSize校准对象
/// </summary>
[Serializable]
public sealed class CalibrationMicroscopePixelSizeItem : CalibrationBase
{
    /// <summary>
    /// 显微镜镜头
    /// </summary>
    public CgMicroscopeLens CgMicroscopeLens { get; set; }

    /// <summary>
    /// 当前镜头的X方向和Y方向1像素转尺寸, 单位um/pixel(SizePerPixel), **Cuga内部使用**
    /// </summary>
    public CgSize PixelSize { get; set; }
}
```

## 2.4. 中心校准: `CalibrationMicroscopeCentricityItem`

> 根据不同 `列表.SingleOrDefault(t => t.CgMicroscopeLens == 镜头)` 判断`is not null`后使用
> 
> 个数：5

```csharp
/// <summary>
/// Centricity校准对象
/// </summary>
[Serializable]
public sealed class CalibrationMicroscopeCentricityItem : CalibrationBase
{
    /// <summary>
    /// 显微镜镜头
    /// </summary>
    public CgMicroscopeLens CgMicroscopeLens { get; set; }

    /// <summary>
    /// 当前镜头的中心位置偏差[相对距离](都是和150X偏差显微镜(当前镜头 - 150X镜头), 所以可以通过这个可以计算任意两个镜头差), **Cuga内部使用**
    /// </summary>
    public CgPoint Offset { get; set; }
}
```

# 3. Chuck: `CalibrationChuckObj`

---

```cs
/// <summary>
/// Chuck校准对象
/// </summary>
[Serializable]
public sealed class CalibrationChuckObj
{
    /// <summary>
    /// Gantry正交校准对象
    /// </summary>
    [Description(WcfConstantHelper.ChuckGantryCalibrationName)]
    public CalibrationChuckGantry CalibrationChuckGantry { get; set; } = new CalibrationChuckGantry();

    /// <summary>
    /// Chuck 全局比例误差校准对象
    /// </summary>
    [Description(WcfConstantHelper.ChuckGlobalScaleErrorCalibrationName)]
    public CalibrationChuckGlobalScaleError CalibrationChuckGlobalScaleError { get; set; } = new CalibrationChuckGlobalScaleError();

    /// <summary>
    /// Chuck Center校准对象
    /// </summary>
    [Description(WcfConstantHelper.ChuckCenterCalibrationName)]
    public CalibrationCenterObj CalibrationCenterObj { get; set; } = new CalibrationCenterObj();

    /// <summary>
    /// Prealigner校准对象
    /// </summary>
    [Description(WcfConstantHelper.ChuckPrealignerCalibrationName)]
    public CalibrationPrealignerObj CalibrationPrealignerObj { get; set; } = new CalibrationPrealignerObj();

   /// <summary>
   /// Chuck 旋转比例误差校准对象
   /// </summary>
   [Description(WcfConstantHelper.ChuckRotateScaleErrorCalibrationName)]
   public CalibrationChuckRotateScaleError CalibrationChuckRotateScaleError { get; set; } = new CalibrationChuckRotateScaleError();

    /// <summary>
    /// Stage Map 校准对象
    /// </summary>
    [Description(WcfConstantHelper.ChucStageMapCalibrationName)]
    public CalibrationChuckStageMap CalibrationChuckStageMap { get; set; } = new CalibrationChuckStageMap();
}
```

## 3.1. 正交校准: `CalibrationChuckGantry`

```csharp
/// <summary>
/// Gantry正交校准对象
/// </summary>
public sealed class CalibrationChuckGantry : CalibrationBase
{
    /// <summary>
    /// 此显微镜镜头下做的校准
    /// </summary>
    public CgMicroscopeLens CgMicroscopeLens { get; set; }

    /// <summary>
    /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, 整个Y轴的误差(绝对偏差), **需要下发ACS硬件**
    /// </summary>
    public double Offset { get; set; }
}
```

## 3.2. 比例校准:`CalibrationChuckGlobalScaleError`

```csharp
/// <summary>
/// 全局比例误差校准对象
/// </summary>
[Serializable]
public sealed class CalibrationChuckGlobalScaleError : CalibrationBase
{
    /// <summary>
    /// 此显微镜镜头下做的校准
    /// </summary>
    public CgMicroscopeLens CgMicroscopeLens { get; set; }

    /// <summary>
    /// 基于<see cref="CgMicroscopeLens"/>倍镜下, X轴比例误差系数, **Cuga内部使用**
    /// </summary>
    public double ScaleX { get; set; }

    /// <summary>
    /// 基于<see cref="CgMicroscopeLens"/>倍镜下, Y轴比例误差系数, **Cuga内部使用**
    /// </summary>
    public double ScaleY { get; set; }
}
```

## 3.3. Chuck Center校准:`CalibrationCenterObj`

```c#
/// <summary>
/// Chuck Center校准对象
/// </summary>
[Serializable]
public sealed class CalibrationCenterObj : CalibrationBase
{
    /// <summary>
    /// 此显微镜镜头下做的校准
    /// </summary>
    public CgMicroscopeLens CgMicroscopeLens { get; set; }

    /// <summary>
    /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, 校准后明场中心Stage的物理坐标, **Cuga内部使用以及下发AF硬件**
    /// </summary>
    public CgPoint NewBFCenterStagePosition { get; set; }
}
```

## 3.4. Prealigner校准:`CalibrationPrealignerObj`

```c#
/// <summary>
/// Prealigner校准对象
/// </summary>
[Serializable]
public sealed class CalibrationPrealignerObj : CalibrationBase
{
    /// <summary>
    /// 此显微镜镜头下做的校准
    /// </summary>
    public CgMicroscopeLens CgMicroscopeLens { get; set; }

    /// <summary>
    /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, 校准后EFEM上下料位置的Stage物理坐标, **Cuga内部使用**
    /// </summary>
    public CgPoint NewEfemLoadWaferStagePosition { get; set; }

    /// <summary>
    /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, 校准后EFEM上下料时Chuck的起始旋转角度（绝对角度）, **Cuga内部使用**
    /// </summary>
    public double EfemLoadWaferChuckAbsoluteAngle { get; set; }
}
```

## 3.5. 旋转比例校准:`CalibrationChuckRotateScaleError`

```c#
/// <summary>
/// 旋转比例误差校准对象
/// </summary>
[Serializable]
public sealed class CalibrationChuckRotateScaleError : CalibrationBase
{
    /// <summary>
    /// 此显微镜镜头下做的校准
    /// </summary>
    public CgMicroscopeLens CgMicroscopeLens { get; set; }

    /// <summary>
    /// 基于<see cref="CgMicroscopeLens"/>倍镜下, T轴比例误差系数, **Cuga内部使用**
    /// </summary>
    public double ScaleT { get; set; }
}
```

## 3.6. Stage Map校准:`CalibrationChuckDarkFieldStageMap`

> 使用`ExpandStageMap.ToCgErrorMap()`下发

```csharp
/// <summary>
/// Stage Map 校准对象
/// </summary>
[Serializable]
public sealed class CalibrationChuckStageMap : CalibrationBase
{
    /// <summary>
    /// 此显微镜镜头下做的校准
    /// </summary>
    public CgMicroscopeLens CgMicroscopeLens { get; set; }

    /// <summary>
    /// 此暗场Mag类型下做的校准
    /// </summary>
    public CgMagTypeEnum OpticsMagTypeEnum { get; set; }

    /// <summary>
    /// 此速度下做的校准
    /// </summary>
    public ADSSpeedEnum Speed { get; set; }

    /// <summary>
    /// 基于<see cref="CgMicroscopeLens"/>倍镜, <see cref="OpticsMagTypeEnum"/>Mag, <see cref="Speed"/>速度下做的校准. StageMap矩阵(笛卡尔坐标系), **需要下发ACS硬件**
    /// </summary>
    public StageMap ExpandStageMap { get; set; }
}

/// <summary>
/// StageMap矩阵(笛卡尔坐标系)
/// </summary>
[Serializable]
public sealed class StageMap
{
    /// <summary>
    /// 理想矩阵的起点
    /// </summary>
    public CgPoint IdealBasePoint { get; set; }

    /// <summary>
    /// 行数量
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// 列数量
    /// </summary>
    public int ColumnNumber { get; set; }

    /// <summary>
    /// 列宽
    /// </summary>
    public double ColumnCellWidth { get; set; }

    /// <summary>
    /// 行高
    /// </summary>
    public double RowCellHeight { get; set; }

    /// <summary>
    /// 误差矩阵
    /// </summary>
    public CgPoint[][] ErrorMatrix { get; set; }

    /// <summary>
    /// 转换为Cuga下发ACS硬件的ErrorMap
    /// </summary>
    /// <returns>Cuga下发ACS硬件的ErrorMap</returns>
    public CgErrorMap ToCgErrorMap()
    {
        return new CgErrorMap
        {
            Zone = 0,
            BaseX = IdealBasePoint.X,
            BaseY = IdealBasePoint.Y,
            XStep = ColumnCellWidth,
            YStep = RowCellHeight,
            Rows = Enumerable.Range(0, RowNumber).Select(row => new CgErrorMapRow
            {
                Id = row,
                Cols = Enumerable.Range(0, ColumnNumber).Select(column => new CgErrorMapCol
                {
                    Id = column,
                    Location = ErrorMatrix[row][column]
                }).ToList()
            }).ToList()
        };
    }
}
```

# 4. 激光: `CalibrationLaserObj`

---

```csharp
/// <summary>
/// 激光校准对象
/// </summary>
[Serializable]
public sealed class CalibrationLaserObj
{
    /// <summary>
    /// 暗场自动聚焦, AB两路灯亮度校准对象
    /// </summary>
    public CalibrationLaserAutoFocus CalibrationLaserAutoFocus { get; set; } = new();

    /// <summary>
    /// 台面功率计校准对象
    /// </summary>
    public CalibrationLaserOpticalPower[] CalibrationLaserOpticalPowerList { get; set; } = [];

    /// <summary>
    /// Attenuator校准对象
    /// </summary>
    public CalibrationAttenuatorObj[] CalibrationAttenuatorList { get; set; } = [];

    /// <summary>
    /// AOD延迟时间校准对象列表
    /// </summary>
    public CalibrationLaserAodDelayItem[] CalibrationLaserAodDelayItemList { get; set; } = [];

    /// <summary>
    /// 暗场相机的Y像素尺寸校准对象列表
    /// </summary>
    public CalibrationLaserPixelSizeItem[] CalibrationLaserPixelSizeItemList { get; set; } = [];

    /// <summary>
    /// XPixelSizer校准对象
    /// </summary>
    public CalibrationLaserXPixelSizeItem[] CalibrationLaserXPixelSizeList { get; set; } = [];

    /// <summary>
    /// 暗场相机的光斑中心校准对象列表
    /// </summary>
    public CalibrationLaserLineCentricityItem[] CalibrationLaserLineCentricityItemList { get; set; } = [];

    /// <summary>
    /// 暗场相机的Swath扫描正反向误差校准对象列表
    /// </summary>
    public CalibrationCIBLineOrientationOffsetItem[] CalibrationLaserLineOrientationOffsetItemList { get; set; } = [];

    /// <summary>
    /// 暗场DOE角度校准对象
    /// </summary>
    public CalibrationLaserDOEAngle[] CalibrationLaserDoeAngleItems { get; set; } = [];

    /// <summary>
    /// CIB MMD 校准对象列表
    /// </summary>
    public CalibrationLaserCIBMMDItem[] CalibrationLaserCIBMMDItems { get; set; } = [];

    /// <summary>
    /// CIB Light Matching 校准对象列表
    /// </summary>
    public CalibrationLaserCIBLightMatchingItem[] CalibrationLaserCIBLightMatchingItems { get; set; } = [];

    /// <summary>
    /// CIB Illumination Profile 校准对象列表
    /// </summary>
    public CalibrationLaserCIBIlluminationProfileItem[] CalibrationLaserCIBIlluminationProfileItems { get; set; } = [];

    /// <summary>
    /// CIB XTC 校准对象列表
    /// </summary>
    public CalibrationLaserCIBXTCItem[] CalibrationLaserCIBXTCItems { get; set; } = [];

    /// <summary>
    /// CIB AGC Delay 校准对象列表
    /// </summary>
    public CalibrationLaserCIBAGCDelayItem[] CalibrationLaserCIBAGCDelayItems { get; set; } = [];

    /// <summary>
    /// AOD Uniformity 校准对象列表
    /// </summary>
    public CalibrationLaserAODUniformityItem[] CalibrationLaserAODUniformityItems { get; set; } = [];

    /// <summary>
    /// Optics Relay 校准对象列表
    /// </summary>
    public CalibrationOpticsRelay[] CalibrationOpticsRelays { get; set; } = [];

    /// <summary>
    /// Optics INC 校准对象列表
    /// </summary>
    public CalibrationOpticsINC[] CalibrationOpticsINCs { get; set; } = [];

    /// <summary>
    /// 采集偏振校准, CollectionPolarization对象数据
    /// </summary>
    public CalibrationCollectionPolarization CalibrationCollectionPolarization { get; set; } = new CalibrationCollectionPolarization();
}
```

## 4.1. 自动聚焦校准: `CalibrationLaserAutoFocus`

```csharp
/// <summary>
/// 暗场自动聚焦, AB两路灯亮度校准对象
/// </summary>
[Serializable]
public sealed class CalibrationLaserAutoFocus : CalibrationBase
{
    /// <summary>
    /// 中档: A路灯的电流值(绝对电流值), **需要下发AF硬件**
    /// </summary>
    public double MiddleCurrentA { get; set; }

    /// <summary>
    /// 中档: B路灯的电流值(绝对电流值), **需要下发AF硬件**
    /// </summary>
    public double MiddleCurrentB { get; set; }

    /// <summary>
    /// 低档系数
    /// </summary>
    public double LowCoefficient { get; set; } = 0.6d;

    /// <summary>
    /// 高档系数
    /// </summary>
    public double HighCoefficient { get; set; } = 1.5d;

    /// <summary>
    /// 低档: A路灯的电流值(绝对电流值), **需要下发AF硬件**
    /// </summary>
    public double LowCurrentA => LowCoefficient * MiddleCurrentA;

    /// <summary>
    /// 低档: B路灯的电流值(绝对电流值), **需要下发AF硬件**
    /// </summary>
    public double LowCurrentB => LowCoefficient * MiddleCurrentB;

    /// <summary>
    /// 高档: A路灯的电流值(绝对电流值), **需要下发AF硬件**
    /// </summary>
    public double HighCurrentA => HighCoefficient * MiddleCurrentA;

    /// <summary>
    /// 高档: B路灯的电流值(绝对电流值), **需要下发AF硬件**
    /// </summary>
    public double HighCurrentB => HighCoefficient * MiddleCurrentB;

    /// <summary>
    /// Nsc 增益归一化, **需要下发AF硬件** 【需要 * 1000下发】
    /// </summary>
    public double NscGain { get; set; }

    /// <summary>
    /// 斜率(ECS/mm), **Cuga内部使用**
    /// </summary>
    public double Slope { get; set; }

    /// <summary>
    /// AF 工作范围 最小值, **Cuga内部使用**
    /// </summary>
    public double MinAFMotorAbsoluteValue { get; set; }

    /// <summary>
    /// AF 工作范围 最大值, **Cuga内部使用**
    /// </summary>
    public double MaxAFMotorAbsoluteValue { get; set; }
}
```

## 4.2. Laser 台面功率计校准：`CalibrationLaserOpticalPower`

> 根据不同 `列表.SingleOrDefault(t => t.CgNIOITypeEnum ==OI/NI && t.CgMagTypeEnum == 暗场Mag)` 判断`is not null`后使用
> 
> 个数：  OI 3 NI 2

```c#
/// <summary>
/// 台面功率计校准对象
/// </summary>
[Serializable]
public sealed class CalibrationLaserOpticalPower : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 测试的功率系数
    /// </summary>
    public double Coefficient { get; set; }

    /// <summary>
    /// 当前暗场Mag的测量的最大功率, **Cuga内部使用**
    /// </summary>
    public double MeasureMaxPower { get; set; }

    /// <summary>
    /// 当前暗场Mag的测量的最大功率机械坐标, **Cuga内部使用**
    /// </summary>
    public CgPoint MeasureMaxPowerPosition { get; set; }
}
```

## ==4.3.== Laser 台面功率曲线校准：`CalibrationAttenuatorObj`

> 根据不同 `列表.SingleOrDefault(t => t.CgNIOITypeEnum ==OI/NI && t.CgMagTypeEnum == 暗场Mag)` 判断`is not null`后使用
> 
> 个数：   OI 3 NI 2

```c#
/// <summary>
/// Attenuator校准对象
/// </summary>
[Serializable]
public sealed class CalibrationAttenuatorObj : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 最大的功率系数台面功率计的平均值P
    /// </summary>
    public double MaxCoefficientAverageMeasurePower { get; set; }

    /// <summary>
    /// 台面功率与系数C之间的曲线值(C,Pc)曲线
    /// </summary>
    public IReadOnlyList<CgPoint> CoefficientMeasurePowerPoints { get; set; }

    /// <summary>
    /// 台面功率与系数C之间的曲线值(C,Pc/P)曲线
    /// </summary>
    public IReadOnlyList<CgPoint> CoefficientMeasurePowerRatePoints { get; set; }

    /// <summary>
    /// 系数曲线通过三次多项式拟合的系数: 0次方
    /// </summary>
    public double P0 { get; set; }

    /// <summary>
    /// 系数曲线通过三次多项式拟合的系数: 1次方
    /// </summary>
    public double P1 { get; set; }

    /// <summary>
    /// 系数曲线通过三次多项式拟合的系数: 2次方
    /// </summary>
    public double P2 { get; set; }

    /// <summary>
    /// 系数曲线通过三次多项式拟合的系数: 3次方
    /// </summary>
    public double P3 { get; set; }

    /// <summary>
    /// 系数曲线通过三次多项式拟合的相关系数
    /// </summary>
    public double RSquared { get; set; }

    /// <summary>
    /// 饱和系数
    /// </summary>
    public double SaturationCoefficient { get; set; }

    /// <summary>
    /// 系数曲线通过三次多项式拟合后的曲线值
    /// </summary>
    public IReadOnlyList<CgPoint> CoefficientFitMeasurePowerRatePoints { get; set; }

    /// <summary>
    /// 校准间隔时间s
    /// </summary>
    public double WaitTime { get; set; }
}
```

## 4.4.  AOD 延迟校准: `CalibrationLaserAodDelayItem`

> 根据不同 `列表.SingleOrDefault(t => t.CgNIOITypeEnum ==OI/NI && t.CgMagTypeEnum == 暗场Mag)` 判断`is not null`后使用
> 
> 个数： OI 3 NI 2

```csharp
/// <summary>
/// AOD延迟时间校准对象
/// </summary>
public sealed class CalibrationLaserAodDelayItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// 暗场Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 当前暗场Mag的Prescan AOD 延迟时间(绝对延迟时间), **需要下发Laser硬件**
    /// </summary>
    public double PrescanAodDelayTime { get; set; }

    /// <summary>
    /// 当前暗场Mag的Chirp AOD 延迟时间(绝对延迟时间), **需要下发Laser硬件**
    /// </summary>
    public double ChirpAodDelayTime { get; set; }
}
```

## 4.5. AOD 散光校准：`CalibrationLaserXYAstigmatismItem`

> 根据不同 `列表.SingleOrDefault(t => t.CgMagTypeEnum == 暗场Mag)` 判断`is not null`后使用
> 
> 个数：3

```
/// <summary>
/// AOD暗场散光校准对象
/// </summary>
public sealed class CalibrationLaserXYAstigmatismItem : CalibrationBase
{
    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 当前Mag校准下的Chirp AOD波形文件路径, **需要下发Laser硬件**
    /// </summary>
    public string ChirpAodWaveFilePath { get; set; } = string.Empty;
}
```

## 4.6. AOD Prescan均匀性校准: `CalibrationLaserAODUniformityItem`

> 根据不同 `列表.SingleOrDefault(t => t => t.CgNIOITypeEnum ==OI/NI && t.CgMagTypeEnum == 暗场Mag && t.CgMagTypeEnum == 幅值)` 判断`is not null`后使用
> 
> 个数： 2 * 3 * 13 = 78
> 
> 当前里面的single后，`波形数组 * Uniformities * OpticsPolarizationModeEnumMeasurePowers[当前偏振] / OpticsPolarizationModeEnumMeasurePowers[OpticsPolarizationModeEnum]`，

```cs
/// <summary>
/// AOD Uniformitiy 校准
/// </summary>
[Serializable]
public sealed class CalibrationLaserAODUniformityItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 波形功率系数(1表示100%, 0表示0%)
    /// </summary>
    public double Coefficient { get; set; }

    /// <summary>
    /// Uniformity 偏振功率校准结果, **需要下发AOD硬件**
    /// </summary>
    public IReadOnlyList<KeyValuePair<CgPolarizationTypeEnum, double>> OpticsPolarizationModeEnumMeasurePowers { get; set; }

    /// <summary>
    /// Uniformitiy 校准结果使用的偏振
    /// </summary>
    public CgPolarizationTypeEnum OpticsPolarizationModeEnum { get; set; }

    /// <summary>
    /// Uniformity 校准结果, **需要下发AOD硬件**
    /// </summary>
    public IReadOnlyList<double> Uniformities { get; set; }
}
```

## ==4.7.== CIB的采样窗口完全同步校准: `CalibrationLaserCIBXTCItem`

根据不同 `列表.SingleOrDefault(t => t => t.CgNIOITypeEnum ==OI/NI && t.CgMagTypeEnum == 暗场Mag)` 判断`is not null`后使用

`Items`属性按照`列表.SingleOrDefault(t.PMTId== PMTId && t.ChannelId== ChannelId)`判断`is not null`后使用

个数：OI 3 NI 2

Items个数：15 * 3 = 45

```cs
/// <summary>
/// CIB XTC 校准
/// </summary>
[Serializable]
public sealed class CalibrationLaserCIBXTCItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 校准结果, **需要下发CIB硬件**
    /// </summary>
    public IReadOnlyList<Item> Items { get; set; }

    /// <summary>
    /// 每个CIB的校准结果
    /// </summary>
    public sealed class Item
    {
        /// <summary>
        /// CIB PMT ID
        /// </summary>
        public int PMTId { get; set; }

        /// <summary>
        /// CIB Channel ID
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// 延迟 PMTDelay SenseDelay, **需要下发CIB硬件**
        /// </summary>
        public double Delay { get; set; }
    }
}
```

## 4.8. CIB 暗场相机Y像素尺寸校准: `CalibrationLaserPixelSizeItem`

> 根据不同 `列表.SingleOrDefault(t => t.CgNIOITypeEnum ==OI/NI && t.CgMagTypeEnum == 暗场Mag && t.PmtId == PmtId)` 判断`is not null`后使用
> 
> 个数： OI 3 * 15 = 45 NI 2 * 15 =30

```csharp
/// <summary>
/// 暗场相机的Y像素尺寸校准对象
/// </summary>
[Serializable]
public sealed class CalibrationLaserPixelSizeItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// 暗场Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 暗场相机ID
    /// </summary>
    public int PmtId { get; set; }

    /// <summary>
    /// 当前暗场Mag和PmtId下的Y方向1像素转尺寸, 单位um/pixel, **Cuga内部使用**
    /// </summary>
    public double YPixelSize { get; set; }
}
```

## 4.9. CIB 暗场相机X像素尺寸校准: `CalibrationLaserXPixelSizeItem`

> 根据不同 `列表.SingleOrDefault(t => t.CgNIOITypeEnum ==OI/NI && t.CgMagTypeEnum == 暗场Mag && t.Speed == 速度)` 判断`is not null`后使用
> 
> 个数：OI 3 * 3  = 9 NI 2 * 1 = 1

```csharp
/// <summary>
/// 暗场相机的X像素尺寸校准对象
/// </summary>
[Serializable]
public sealed class CalibrationLaserXPixelSizeItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    /// <summary>
    /// 当前暗场Mag和速度下的X方向1像素转尺寸, 单位um/pixel, **Cuga内部使用**
    /// </summary>
    public double XPixelSize { get; set; }
}
```

## 4.10. CIB 明暗场中心的offset校准: `CalibrationLaserLineCentricityItem`

> 根据不同 `列表.SingleOrDefault(t => t.CgNIOITypeEnum ==OI/NI && t.CgMagTypeEnum == 暗场Mag && t.Speed == 速度 && t.PmtId == PmtId)` 判断`is not null`后使用
> 
> 个数： OI 3 * 3 * 15 = 135 NI 2 * 1 * 15 = 30

```cs
/// <summary>
/// 中心与明场中心的offset校准
/// </summary>
[Serializable]
public sealed class CalibrationLaserLineCentricityItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// 此显微镜镜头下做的校准
    /// </summary>
    public CgMicroscopeLens CgMicroscopeLens { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    /// <summary>
    /// 暗场相机ID
    /// </summary>
    public int PmtId { get; set; }

    /// <summary>
    /// 当前暗场Mag和速度PmtId下的基于<see cref="CgMicroscopeLens"/>倍镜下, 正向暗场中心坐标, **Cuga内部使用, 8号光斑需要下发到AF硬件**
    /// </summary>
    public CgPoint DarkMachineCenterPosition { get; set; }
}
```

## 4.11. CIB 正反向校准：`CalibrationCIBLineOrientationOffsetItem`

> 根据不同 `列表.SingleOrDefault(t => t.CgNIOITypeEnum ==OI/NI && t.CgMagTypeEnum == 暗场Mag && t.Speed == 速度 && t.PmtId == PmtId)` 判断`is not null`后使用（当前只保留中心光斑的结果）

```csharp
/// <summary>
/// 正反向扫描offset校准(机械坐标差值)
/// </summary>
[Serializable]
public sealed class CalibrationCIBLineOrientationOffsetItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    /// <summary>
    /// 暗场相机ID
    /// </summary>
    public int PmtId { get; set; }

    /// <summary>
    /// 当前暗场CgNIOIType、Mag和速度PmtId下的正反向误差值
    /// </summary>
    public double XOffset { get; set; }
}
```

## 4.12. CIB MMD校准: `CalibrationLaserCIBMMDItem`

根据不同 `列表.SingleOrDefault(t => t.PMTId== PMTId && t.ChannelId== ChannelId)` 判断`is not null`后使用

个数：15 * 3 = 45

```csharp
/// <summary>
/// CIB MMD 校准
/// </summary>
[Serializable]
public sealed class CalibrationLaserCIBMMDItem : CalibrationBase
{
    /// <summary>
    /// CIB PMT ID
    /// </summary>
    public int PMTId { get; set; }

    /// <summary>
    /// CIB Channel ID
    /// </summary>
    public int ChannelId { get; set; }

    /// <summary>
    /// LogGain * 128 [0, 2^12-1], **需要下发CIB硬件**, 且最大值, **需要下发CIB硬件**
    /// </summary>
    public IReadOnlyList<double> LogGainMul128U12Bits { get; set; }

    /// <summary>
    /// GainS16Bit [-2^15, 2^15-1], **需要下发CIB硬件**
    /// </summary>
    public IReadOnlyList<double> GainS16Bits { get; set; }
}
```

## 4.13. CIB的AGC完全同步校准: `CalibrationLaserCIBAGCDelayItem`

根据不同 `列表.SingleOrDefault(t => t => t.CgNIOITypeEnum ==OI/NI && t.CgMagTypeEnum == 暗场Mag)` 判断`is not null`后使用

`Items`属性按照`列表.SingleOrDefault(t.PMTId== PMTId && t.ChannelId== ChannelId)`判断`is not null`后使用

个数：OI 3 NI 2

Items个数：15 * 3 = 45

```cs
/// <summary>
/// CIB AGC Delay 校准
/// </summary>
[Serializable]
public sealed class CalibrationLaserCIBAGCDelayItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    public string CgNIOIType => CgNIOITypeEnum.ToString();

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    public string CgMagType => CgMagTypeEnum.ToString();

    /// <summary>
    /// 校准结果, **需要下发CIB硬件**
    /// </summary>
    public IReadOnlyList<Item> Items { get; set; }

    /// <summary>
    /// 每个CIB的校准结果
    /// </summary>
    public sealed class Item
    {
        /// <summary>
        /// CIB PMT ID
        /// </summary>
        public int PMTId { get; set; }

        /// <summary>
        /// CIB Channel ID
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// 延迟 DACDealy, **需要下发CIB硬件**
        /// </summary>
        public double Delay { get; set; }
    }
}
```

## 4.14. DOE Angle

```cs
/// <summary>
/// DOE角度校准
/// </summary>
[Serializable]
public sealed class CalibrationLaserDOEAngle : CalibrationBase
{
    public double DOEAngle { get; set; }
}
```

## 4.15. CIB Light Matching校准: `CalibrationLaserCIBLightMatchingItem`

根据不同 `列表.SingleOrDefault(t => t => t.CgNIOITypeEnum ==OI/NI && t.CgMagTypeEnum == 暗场Mag && t.Speed == 速度 && t.OpticsApodizationModeEnum == 切趾 && t.OpticsPolarizationModeEnum == 光学偏振 && t.CollectorPolarizationModeEnum == 采集偏振)` 判断`is not null`后使用

`Items`属性按照`列表.SingleOrDefault(t.PMTId== PMTId && t.ChannelId== ChannelId)`判断`is not null`后使用

个数：OI 3 * 3 * 1(切趾待定) * 3 * 3  = 81 NI 2 * 1 * 1(切趾待定) * 3 * 3 = 18

Items个数：15 * 3 = 45

```csharp
/// <summary>
/// CIB Light Matching 校准
/// </summary>
[Serializable]
public sealed class CalibrationLaserCIBLightMatchingItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    /// <summary>
    /// 光学 Apodization
    /// </summary>
    public int OpticsApodizationModeEnum { get; set; }

    /// <summary>
    /// 光学偏振
    /// </summary>
    public CgPolarizationTypeEnum OpticsPolarizationModeEnum { get; set; }

    /// <summary>
    /// 采集偏振
    /// </summary>
    public CgNDFTypeEnum CollectorPolarizationModeEnum { get; set; }

    /// <summary>
    /// 校准结果, **需要下发CIB硬件**
    /// </summary>
    public IReadOnlyList<Item> Items { get; set; }

    /// <summary>
    /// 每个CIB的校准结果
    /// </summary>
    public sealed class Item
    {
        /// <summary>
        /// CIB PMT ID
        /// </summary>
        public int PMTId { get; set; }

        /// <summary>
        /// CIB Channel ID
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// 数码增益, **需要下发CIB硬件**
        /// </summary>
        public double DigitalGainPlusMultiplicativeFactors { get; set; }
    }
}
```

## ==4.16.== CIB Illumination Profile 校准: `CalibrationLaserCIBIlluminationProfileItem`

根据不同 `列表.SingleOrDefault(t => t => t.CgNIOITypeEnum ==OI/NI && t.CgMagTypeEnum == 暗场Mag && t.Speed == 速度 && t.OpticsApodizationModeEnum == 切趾 && t.OpticsPolarizationModeEnum == 光学偏振 && t.CollectorPolarizationModeEnum == 采集偏振)` 判断`is not null`后使用

`Items`属性按照`列表.SingleOrDefault(t.PMTId== PMTId && t.ChannelId== ChannelId)`判断`is not null`后使用

个数：OI 3 * 3 * 1(切趾待定) * 3 * 3  = 81 NI 2 * 1 * 1(切趾待定) * 3 * 3 = 18

Items个数：15 * 3 = 45

```csharp
/// <summary>
/// CIB Illumination Profile 校准
/// </summary>
[Serializable]
public sealed class CalibrationLaserCIBIlluminationProfileItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    /// <summary>
    /// 光学 Apodization
    /// </summary>
    public int OpticsApodizationModeEnum { get; set; }

    /// <summary>
    /// 光学偏振
    /// </summary>
    public CgPolarizationTypeEnum OpticsPolarizationModeEnum { get; set; }

    /// <summary>
    /// 采集偏振
    /// </summary>
    public CgNDFTypeEnum CollectorPolarizationModeEnum { get; set; }

    /// <summary>
    /// 校准结果, **需要下发CIB硬件**
    /// </summary>
    public IReadOnlyList<Item> Items { get; set; }

    /// <summary>
    /// 每个CIB的校准结果
    /// </summary>
    public sealed class Item
    {
        /// <summary>
        /// CIB PMT ID
        /// </summary>
        public int PMTId { get; set; }

        /// <summary>
        /// CIB Channel ID
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// 均匀性校准结果, **需要下发CIB硬件**
        /// </summary>
        public IReadOnlyList<double> IlluminationProfiles { get; set; }
    }
}
```

## 4.17. Optics Relay校准: `CalibrationOpticsRelay`

> 根据不同 `列表.SingleOrDefault(t => t.CgNIOITypeEnum ==OI/NI)` 判断`is not null`后使用
> 
> 个数： 2

```cs
/// <summary>
/// Optics Relay 校准
/// </summary>
[Serializable]
public sealed class CalibrationOpticsRelay : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// 斜率(ECS/mm), **Cuga内部使用**
    /// </summary>
    public double Slope { get; set; }

    /// <summary>
    /// Relay 工作范围 最小值, **Cuga内部使用**
    /// </summary>
    public double MinRelayMotorAbsoluteValue { get; set; }

    /// <summary>
    /// Relay 工作范围 最大值, **Cuga内部使用**
    /// </summary>
    public double MaxRelayMotorAbsoluteValue { get; set; }
}
```

## ==4.18.== Optics INC校准: `CalibrationOpticsRelay`

> 根据不同 `列表.SingleOrDefault(t => t.CgNIOITypeEnum ==OI/NI && t.CgMagTypeEnum == 暗场Mag && t.Speed == 速度)` 判断`is not null`后使用
> 
> 个数： OI 3 * 3 = 9 NI 2 * 1 = 2

```CS
/// <summary>
/// Optics INC 校准
/// </summary>
[Serializable]
public sealed class CalibrationOpticsINC : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    /// <summary>
    /// INC电机位置, **需要下发Optics Motor硬件**
    /// </summary>
    public double? INCMotorAbsoluteValue { get; set; }
}
```

## ==4.19.== 采集偏振校准: `CollectionPolarization`

```csharp
/// <summary>
/// 采集偏振校准, CollectionPolarization校准下发Cuga参数
/// </summary>
[Serializable]
public sealed class CalibrationCollectionPolarization : CalibrationBase
{
    /// <summary>
    /// CH1通道NDF电机的S偏振位置, **需要记录**
    /// </summary> 
    public double PolarizationPositionNDFSCH1 { get; set; }

    /// <summary>
    /// CH2通道NDF电机的S偏振位置, **需要记录**
    /// </summary> 
    public double PolarizationPositionNDFSCH2 { get; set; }

    /// <summary>
    /// CH3通道NDF电机的S偏振位置, **需要记录**
    /// </summary> 
    public double PolarizationPositionNDFSCH3 { get; set; }

    /// <summary>
    /// CH1通道NDF电机的P偏振位置, **需要记录**
    /// </summary> 
    public double PolarizationPositionNDFPCH1 { get; set; }

    /// <summary>
    /// CH2通道NDF电机的P偏振位置, **需要记录**
    /// </summary> 
    public double PolarizationPositionNDFPCH2 { get; set; }

    /// <summary>
    /// CH3通道NDF电机的P偏振位置, **需要记录**
    /// </summary> 
    public double PolarizationPositionNDFPCH3 { get; set; }
}
```

# 5. 自动聚焦: `CalibrationAutoFocusObj`

---

```csharp
/// <summary>
/// AutoFocus校准对象
/// </summary>
[Serializable]
public sealed class CalibrationAutoFocusObj
{
    /// <summary>
    /// GFO校准对象列表
    /// </summary>
    public CalibrationAutoFocusGlobalFocusOffset[] CalibrationAutoFocusGlobalFocusOffsets { get; set; } = [];

    /// <summary>
    /// CalChipFocusOffset校准对象列表
    /// </summary>
    public CalibrationAutoFocusCalChipFocusOffset CalibrationAutoFocusCalChipFocusOffset { get; set; } = new();
}
```

## ==5.1.== 暗场焦点位置校准: `CalibrationGlobalFocusOffset`

> 根据不同 `列表.SingleOrDefault(t => t.CgNIOITypeEnum ==OI/NI && t.CgMagTypeEnum == 暗场Mag && t.Speed == 速度)` 判断`is not null`后使用
> 
> 个数： OI 3 * 3 = 9 NI 2 * 1 = 2

```csharp
/// <summary>
/// 暗场CalChip DSW焦点位置校准对象
/// </summary>
[Serializable]
public sealed class CalibrationAutoFocusGlobalFocusOffset : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    /// <summary>
    /// 伺服电机 True:AF, False:Relay, **Cuga内部使用**
    /// </summary>
    public bool IsAFServo { get; set; }

    /// <summary>
    /// 焦点ECS, **Cuga内部使用**
    /// </summary>
    public double ECSValue { get; set; }

     /// <summary>
    /// 电机值, **Cuga内部使用**
    /// </summary>
    public double MotorValue { get; set; }
}
```

## ==5.2.== 暗场`CalChip`校准: `CalibrationAutoFocusCalChipFocusOffset`

```csharp
/// <summary>
/// 暗场Cal Chip焦点位置校准对象
/// </summary>
[Serializable]
public sealed class CalibrationAutoFocusCalChipFocusOffset : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    /// <summary>
    /// 伺服电机 True:AF, False:Relay, **Cuga内部使用**
    /// </summary>
    public bool IsAFServo { get; set; }

   /// <summary>
    /// Chuck暗场最佳Ecs
    /// </summary>
    public double ChuckEcsValue { get; set; }

    /// <summary>
    /// Chuck暗场电机值
    /// </summary>
    public double ChuckMotorValue { get; set; }

    /// <summary>
    /// Dsw暗场最佳Ecs
    /// </summary>
    public double DswEcsValue { get; set; }

    /// <summary>
    /// Dsw暗场电机值
    /// </summary>
    public double DswMotorValue { get; set; }

    /// <summary>
    /// Haze暗场最佳Ecs
    /// </summary>
    public double HazeEcsValue { get; set; }

    /// <summary>
    /// Haze暗场电机值
    /// </summary>
    public double HazeMotorValue { get; set; }
}
```

# 6. 傅里叶校准: `CalibrationPupilFourierObj`

---

```csharp
/// <summary>
/// 傅里叶5个校准对象
/// </summary>
[Serializable]
public sealed class CalibrationPupilFourierObj
{
    /// <summary>
    /// 傅里叶校准, PupilCameraAlignment对象数据
    /// </summary>
    public CalibrationPupilCameraAlignment CalibrationPupilCameraAlignment { get; set; } = new CalibrationPupilCameraAlignment();

    /// <summary>
    /// 傅里叶校准, PupilSideChannelFlexibleAperture对象数据
    /// </summary>
    public CalibrationPupilSideChannelFlexibleAperture CalibrationPupilSideChannelFlexibleAperture { get; set; } = new CalibrationPupilSideChannelFlexibleAperture();

    /// <summary>
    /// 傅里叶校准, PupilCenterChannelFlexibleAperture对象数据
    /// </summary>
    public CalibrationPupilCenterChannelFlexibleAperture CalibrationPupilCenterChannelFlexibleAperture { get; set; } = new CalibrationPupilCenterChannelFlexibleAperture();

    /// <summary>
    /// 傅里叶校准, PupilSideChannelSpecularBlocker对象数据
    /// </summary>
    public CalibrationPupilSideChannelSpecularBlocker CalibrationPupilSideChannelSpecularBlocker { get; set; } = new CalibrationPupilSideChannelSpecularBlocker();

    /// <summary>
    /// 傅里叶校准, PupilCenterChannelSpecularBlocker对象数据
    /// </summary>
    public CalibrationPupilCenterChannelSpecularBlocker CalibrationPupilCenterChannelSpecularBlocker { get; set; } = new CalibrationPupilCenterChannelSpecularBlocker();

}
```

## ==6.1.== 傅里叶PupilCameraAlignment校准: `CalibrationPupilCameraAlignment`

```csharp
/// <summary>
/// 傅里叶PupilCameraAlignment校准下发Cuga参数
/// </summary>
[Serializable]
public sealed class CalibrationPupilCameraAlignment : CalibrationBase
{
    /// <summary>
    /// 通道1截图矩形区域坐标值, **需要记录**
    /// </summary> 
    public Rectangle RectCh1 { get; set; }

    /// <summary>
    /// 通道2截图矩形区域坐标值, **需要记录**
    /// </summary> 
    public Rectangle RectCh2 { get; set; }

    /// <summary>
    /// 通道3截图矩形区域坐标值, **需要记录**
    /// </summary> 
    public Rectangle RectCh3 { get; set; }   
}
```

## ==6.2.== 傅里叶CalibrationPupilSideChannelFlexibleAperture校准: `CalibrationPupilSideChannelFlexibleAperture`

```csharp
/// <summary>
/// 傅里叶PupilSideChannelFlexibleAperture校准下发Cuga参数
/// </summary>
[Serializable]
public sealed class CalibrationPupilSideChannelFlexibleAperture : CalibrationBase
{
    /// <summary>
    /// 傅里叶截图可见区域内第一根杆子起始像素坐标, **不需要记录**
    /// </summary> 
    public Point CgFFBoxBeginPositionCh1 { get; set; }
    public Point CgFFBoxBeginPositionCh2 { get; set; }

    /// <summary>
    /// 傅里叶截图可见区域内最后一根杆子截止像素坐标, **不需要记录**
    /// </summary> 
    public Point CgFFBoxEndPositionCh1 { get; set; }
    public Point CgFFBoxEndPositionCh2 { get; set; }

    /// <summary>
    /// 傅里叶截图可见区域内第一根杆子编号, **需要记录**
    /// </summary> 
    public int CgFFBoxBeginNumber1Ch1 { get; set; }
    public int CgFFBoxBeginNumber2Ch1 { get; set; }
    public int CgFFBoxBeginNumber1Ch2 { get; set; }
    public int CgFFBoxBeginNumber2Ch2 { get; set; }

    /// <summary>
    /// 傅里叶截图可见区域内最后一根杆子编号, **需要记录**
    /// </summary> 
    public int CgFFBoxEndNumber1Ch1 { get; set; }
    public int CgFFBoxEndNumber2Ch1 { get; set; }
    public int CgFFBoxEndNumber1Ch2 { get; set; }
    public int CgFFBoxEndNumber2Ch2 { get; set; }

    /// <summary>
    /// 傅里叶截图可见区域内每根杆子宽度（像素值）, **需要记录**
    /// </summary> 

    public List<int> CgFFBoxRodWidthListCh1 { get; set; }

    public List<int> CgFFBoxRodWidthListCh2 { get; set; }

    /// <summary>
    /// 每根杆子像素高度和真实高度对应比例换算，按照百分比, 每1%相当于多少像素,**需要记录**
    /// </summary> 
    public List<int> CgFFBoxHeightRelationPercentListCh1 { get; set; }
    public List<int> CgFFBoxHeightRelationPercentListCh2 { get; set; }

    /// <summary>
    /// 每根杆子矩形区域像素坐标,**需要记录**
    /// </summary> 
    public List<Rectangle> CurrentImageRectListFirstCh1 { get; set; }

    public List<Rectangle> CurrentImageRectListFirstCh2 { get; set; }

    /// <summary>
    /// 每根杆子起始位置，也是记录的百分比,**需要记录**
    /// </summary> 
    public double CgFFBoxAllRodsBeginPercentCh1 { get; set; }

    public double CgFFBoxAllRodsBeginPercentCh2 { get; set; }
}
```

## ==6.3.== 傅里叶CalibrationPupilSideChannelSpecularBlocker校准: `CalibrationPupilSideChannelSpecularBlocker`

```csharp
/// <summary>
/// 傅里叶CalibrationPupilSideChannelSpecularBlocker校准下发Cuga参数
/// </summary>
[Serializable]
public sealed class CalibrationPupilSideChannelSpecularBlocker : CalibrationBase
{
    /// <summary>
    /// 傅里叶截图可见区域内开始移动的第一根杆子像素坐标, **不需要记录**
    /// </summary> 
    public Point CgFFBoxBeginPositionCh1 { get; set; }
    public Point CgFFBoxBeginPositionCh2 { get; set; }

    /// <summary>
    /// 傅里叶截图可见区域内开始移动的第一根杆子编号, **需要记录**
    /// </summary> 
    public int CgFFBoxBeginNumberCh1 { get; set; }
    public int CgFFBoxBeginNumberCh2 { get; set; }

    /// <summary>
    /// 傅里叶截图可见区域内开始移动的最后一根杆子编号, **需要记录**
    /// </summary> 
    public int CgFFBoxEndNumberCh1 { get; set; }
    public int CgFFBoxEndNumberCh2 { get; set; }

    /// <summary>
    /// 傅里叶截图可见区域内每根杆子向下移动百分比, **需要记录**
    /// </summary> 
    public List<double> CgFFBoxMoveDownPercentListCh1 { get; set; }
    public List<double> CgFFBoxMoveDownPercentListCh2 { get; set; }
}
```

## ==6.4.== 傅里叶CalibrationPupilCenterChannelFlexibleAperture校准: `CalibrationPupilCenterChannelFlexibleAperture`

```csharp
/// <summary>
/// 傅里叶CalibrationPupilCenterChannelFlexibleAperture校准下发Cuga参数
/// </summary>
[Serializable]
public sealed class CalibrationPupilCenterChannelFlexibleAperture : CalibrationBase
{
    /// <summary>
    /// 垂直方向转盘从120度到240度转动4个位置，记录下来每一个角度值,**需要记录**
    /// </summary> 
    public List<double> CgFFBoxTurnXAngleCh3 { get; set; }

    /// <summary>
    /// 垂直方向转盘从120度到240度转动4个位置，记录下来每一个挡杆像素宽度,**需要记录**
    /// </summary> 
    public List<double> CgFFBoxTurnXWidthCh3 { get; set; }

    /// <summary>
    /// 垂直方向转盘从120度到240度转动4个位置，记录下来位移电机每1毫米相当于多少像素宽度,**需要记录**
    /// </summary> 
    public List<double> CgFFBoxTurnXMotorRelationCH3 { get; set; }

    /// <summary>
    /// 垂直方向转盘从120度到240度转动4个位置，记录下来位移电机起始位置（单位为毫米）,**需要记录**
    /// </summary> 
    public List<double> CgFFBoxTurnXMotorPositionCH3 { get; set; }

    /// <summary>
    /// 垂直方向转盘从120度到240度转动4个位置，记录下来每一根挡杆矩形区域坐标,**需要记录**
    /// </summary> 
    public List<Rectangle> CgFFBoxTurnXRectPositionCH3 { get; set; }

    /// <summary>
    /// 垂直方向通光孔算出来的圆形圆心坐标值,**需要记录**
    /// </summary> 
    public Point CgFFBoxTurnXLightHoleCircleCenterCh3 { get; set; }

    /// <summary>
    /// 垂直方向通光孔算出来的圆形半径,**需要记录**
    /// </summary> 
    public double CgFFBoxTurnXLightHoleCircleRadiusCh3 { get; set; }

    /// <summary>
    /// 水平方向转盘从120度到240度转动4个位置，记录下来每一个角度值,**需要记录**
    /// </summary> 
    public List<double> CgFFBoxTurnYAngleCh3 { get; set; }

    /// <summary>
    /// 水平方向转盘从120度到240度转动4个位置，记录下来每一个挡杆像素高度,**需要记录**
    /// </summary> 
    public List<double> CgFFBoxTurnYWidthCh3 { get; set; }

    /// <summary>
    /// 水平方向转盘从120度到240度转动4个位置，记录下来位移电机每1毫米相当于多少像素宽度,**需要记录**
    /// </summary> 
    public List<double> CgFFBoxTurnYMotorRelationCH3 { get; set; }

    /// <summary>
    /// 水平方向转盘从120度到240度转动4个位置，记录下来位移电机起始位置（单位为毫米）,**需要记录**
    /// </summary> 
    public List<double> CgFFBoxTurnYMotorPositionCH3 { get; set; }

    /// <summary>
    /// 水平方向转盘从120度到240度转动4个位置，记录下来每一根挡杆矩形区域坐标,**需要记录**
    /// </summary> 
    public List<Rectangle> CgFFBoxTurnYRectPositionCH3 { get; set; }

    /// <summary>
    /// 水平方向通光孔算出来的圆形圆心坐标值,**需要记录**
    /// </summary> 
    public Point CgFFBoxTurnYLightHoleCircleCenterCh3 { get; set; }

    /// <summary>
    /// 水平方向通光孔算出来的圆形半径,**需要记录**
    /// </summary> 
    public double CgFFBoxTurnYLightHoleCircleRadiusCh3 { get; set; }

    /// <summary>
    /// 垂直方向单独的推杆像素宽度,**需要记录**
    /// </summary> 
    public double CgFFBoxPushXWidthCh3 { get; set; }

    /// <summary>
    /// 垂直方向单独的推杆位移电机每移动1毫米相当于多少像素宽度,**需要记录**
    /// </summary> 
    public double CgFFBoxPushXMotorRelationCH3 { get; set; }

    /// <summary>
    /// 垂直方向单独的推杆起始位置,**需要记录**
    /// </summary> 
    public double CgFFBoxPushXMotorPositionCH3 { get; set; }

    /// <summary>
    /// 垂直方向单独的推杆起始矩形区域坐标,**需要记录**
    /// </summary> 
    public Rectangle CgFFBoxPushXRectPositionCH3 { get; set; }
}
```

## ==6.5.== 傅里叶PupilCenterChannelSpecularBlocker校准: `PupilCenterChannelSpecularBlocker`

```csharp
/// <summary>
/// 傅里叶PupilCenterChannelSpecularBlocker校准下发Cuga参数
/// </summary>
[Serializable]
public sealed class CalibrationPupilCenterChannelSpecularBlocker : CalibrationBase
{
    /// <summary>
    /// 傅里叶CH3通道反射光校准，Y方向转盘位置, **需要记录**
    /// </summary> 
    public float Ch3TurnY { get; set; }

    /// <summary>
    /// 傅里叶CH3通道反射光校准，X方向推杆位置, **需要记录**
    /// </summary> 
    public float Ch3Push { get; set; }
}
```