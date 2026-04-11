using Core.Wcf.Models.Ads;
using Core.Wcf.Models.AutoFocus;
using Core.Wcf.Models.Chuck;
using Core.Wcf.Models.Fourier;
using Core.Wcf.Models.Laser;
using Core.Wcf.Models.Microscope;
using System.ComponentModel;

namespace Core.Wcf.Models;

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

    /// <summary>
    /// 傅里叶校准对象
    /// </summary>
    [Description(WcfConstantHelper.PupilFourierCalibrationName)]
    public CalibrationPupilFourierObj CalibrationPupilFourierObj { get; set; } = new CalibrationPupilFourierObj();
}