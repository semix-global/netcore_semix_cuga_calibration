using System;

namespace Core.Wcf.Models.Ads;

/// <summary>
/// 缓震平台校准对象
/// </summary>
[Serializable]
public sealed class CalibrationAdsObj
{
    /// <summary>
    /// 压力前馈校准对象
    /// </summary>
    public CalibrationAdsPressureGains CalibrationAdsPressureGains { get; set; } = new CalibrationAdsPressureGains();

    /// <summary>
    /// X方向速度前馈校准对象列表
    /// </summary>
    public CalibrationAdsXGainsItem CalibrationAdsXGains { get; set; } = new CalibrationAdsXGainsItem();

    /// <summary>
    /// Y方向速度前馈校准对象
    /// </summary>
    public CalibrationAdsYGainsItem CalibrationAdsYGains { get; set; } = new CalibrationAdsYGainsItem();
}

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