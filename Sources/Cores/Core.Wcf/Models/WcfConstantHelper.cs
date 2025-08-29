namespace Core.Wcf.Models;

public static class WcfConstantHelper
{
    #region 校准大项描述文本

    public const string AdsNodeCalibrationName = "ADS";

    public const string MicroscopeNodeCalibrationName = "Microscope";

    public const string ChuckNodeCalibrationName = "Chuck";

    public const string LaserNodeCalibrationName = "Laser";

    #endregion 校准大项描述文本

    #region ADS校准小项描述文本

    public const string AdsXGainCalibrationName = "X Gains";

    public const string AdsYGainCalibrationName = "Y Gains";

    public const string AdsPressureCalibrationName = "Pressure Gains";

    #endregion ADS校准小项描述文本

    #region Microscope校准小项描述文本

    public const string MicroscopeFocusCalibrationName = "Focus";

    public const string MicroscopeCalChipCalibrationName = "Cal Chip";

    public const string MicroscopePixelSizeCalibrationName = "Pixel Size";

    public const string MicroscopeCentricityCalibrationName = "Centricity";

    #endregion Microscope校准小项描述文本

    #region Chuck校准小项描述文本

    public const string ChuckPrealignerCalibrationName = "Prealigner";

    public const string ChuckGantryCalibrationName = "Gantry";

    public const string ChuckCenterCalibrationName = "Chuck Center";

    public const string ChuckBrightFieldStageMapCalibrationName = "Bright Field StageMap";

    public const string ChuckDarkFieldStageMapCalibrationName = "Dark Field StageMap";

    public const string ChuckStageMapCalibrationName = "Stage Map";

    public const string ChuckGlobalScaleErrorCalibrationName = "GlobalScaleError";

    public const string ChuckRotateScaleErrorCalibrationName = "RotateScaleError";

    #endregion Chuck校准小项描述文本

    #region Laser校准小项描述文本

    public const string LaserAutoFocusCalibrationName = "AutoFocus";

    public const string LaserAodDelayCalibrationName = "Aod Delay";

    public const string LaserPixelSizeCalibrationName = "Laser Y Pixel Size";

    public const string LaserXPixelSizeCalibrationName = "Laser X Pixel Size";

    public const string LaserLineCentricityCalibrationName = "Line Centricity";

    public const string LaserXtcCalibrationName = "XTC";

    public const string LaserAgcDelayCalibrationName = "AGC Delay";

    public const string LaserIlluminationProfileCalibrationName = "Illumination Profile";

    public const string LaserAttenuatorCalibrationName = "Attenuator";

    public const string LaserOpticalPowerCalibrationName = "OpticalPower";

    public const string LaserPmtGainCalibrationName = "PMT Gain";

    public const string LaserXyAstigmatismCalibrationName = "XY Astigmatism";

    public const string LaserFocusShiftCalibrationName = "Focus Shift";

    public const string LaserDOEAngleCalibrationName = "DOE Angle";

    #endregion Laser校准小项描述文本
}