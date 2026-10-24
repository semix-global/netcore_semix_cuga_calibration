using Cuga.Data.DataStruct.DTO.Recipe;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Cuga.Data.DataStruct.Stage;
using System;
using System.Collections.Generic;

namespace Core.Wcf.Models.Fourier;

/// <summary>
/// 傅里叶7个校准对象
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
    public CalibrationPupilSideChannelSpecularBlocker[] CalibrationPupilSideChannelSpecularBlockerItems { get; set; } = [];

    /// <summary>
    /// 傅里叶校准, PupilCenterChannelSpecularBlocker对象数据
    /// </summary>
    public CalibrationPupilCenterChannelSpecularBlocker[] CalibrationPupilCenterChannelSpecularBlockerItems { get; set; } = [];

    /// <summary>
    /// 傅里叶校准, PupilCenterChannelFlexibleApertureV1对象数据
    /// </summary>
    public CalibrationPupilCenterChannelFlexibleApertureV1 CalibrationPupilCenterChannelFlexibleApertureV1 { get; set; } = new CalibrationPupilCenterChannelFlexibleApertureV1();

    /// <summary>
    /// 傅里叶校准, PupilCenterChannelSpecularBlockerV1对象数据
    /// </summary>
    public CalibrationPupilCenterChannelSpecularBlockerV1[] CalibrationPupilCenterChannelSpecularBlockerV1Items { get; set; } = [];
}

/// <summary>
/// 傅里叶PupilCameraAlignment校准下发Cuga参数
/// </summary>
[Serializable]
public sealed class CalibrationPupilCameraAlignment : CalibrationBase
{
    /// <summary>
    /// 通道1截图矩形区域坐标值, **需要记录**
    /// </summary> 
    public RectD RectCh1 { get; set; }

    /// <summary>
    /// 通道2截图矩形区域坐标值, **需要记录**
    /// </summary> 
    public RectD RectCh2 { get; set; }

    /// <summary>
    /// 通道3截图矩形区域坐标值, **需要记录**
    /// </summary> 
    public RectD RectCh3 { get; set; }
}

/// <summary>
/// 傅里叶PupilSideChannelFlexibleAperture校准下发Cuga参数
/// </summary>
[Serializable]
public sealed class CalibrationPupilSideChannelFlexibleAperture : CalibrationBase
{
    /// <summary>
    /// 傅里叶截图可见区域内第一根杆子起始像素坐标, **不需要记录**
    /// </summary> 
    public CgPoint CgFFBoxBeginPositionCh1 { get; set; }

    public CgPoint CgFFBoxBeginPositionCh2 { get; set; }

    /// <summary>
    /// 傅里叶截图可见区域内最后一根杆子截止像素坐标, **不需要记录**
    /// </summary> 
    public CgPoint CgFFBoxEndPositionCh1 { get; set; }

    public CgPoint CgFFBoxEndPositionCh2 { get; set; }

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
    /// 每根杆子每1像素高度相当于多少毫米,**需要记录**
    /// </summary> 
    public List<double> CgFFBoxHeightRelationPercentListCh1 { get; set; }

    public List<double> CgFFBoxHeightRelationPercentListCh2 { get; set; }

    /// <summary>
    /// 每根杆子矩形区域像素坐标,**需要记录**
    /// </summary> 
    public List<RectD> CurrentImageRectListFirstCh1 { get; set; }

    public List<RectD> CurrentImageRectListFirstCh2 { get; set; }

    /// <summary>
    /// 每根杆子起始位置，也是记录的百分比,**需要记录**
    /// </summary> 
    public double CgFFBoxAllRodsBeginPercentCh1 { get; set; }

    public double CgFFBoxAllRodsBeginPercentCh2 { get; set; }
}

/// <summary>
/// 傅里叶CalibrationPupilCenterChannelFlexibleAperture校准下发Cuga参数
/// </summary>
[Serializable]
public sealed class CalibrationPupilCenterChannelFlexibleAperture : CalibrationBase
{
    /// <summary>
    /// X方向转盘从120度到300度转动4个位置，记录下来每一个角度值,**需要记录**
    /// </summary> 
    public List<double> CgFFBoxTurnXAngleCh3 { get; set; }

    /// <summary>
    /// X方向转盘从120度到300度转动4个位置，记录下来每一个挡杆像素宽度,**需要记录**
    /// </summary>
    public List<double> CgFFBoxTurnXWidthCh3 { get; set; }

    /// <summary>
    /// X方向转盘从120度到300度转动4个位置，记录下来每1像素宽度相当于多少毫米,**需要记录**
    /// </summary>
    public List<double> CgFFBoxTurnXMotorRelationCh3 { get; set; }

    /// <summary>
    /// X方向转盘从120度到300度转动4个位置，记录下来位移电机起始位置（单位为毫米）,**需要记录**
    /// </summary>
    public List<double> CgFFBoxTurnXMotorPositionCh3 { get; set; }

    /// <summary>
    /// X方向转盘从120度到300度转动4个位置，记录下来每一根挡杆矩形区域坐标,**需要记录**
    /// </summary>
    public List<RectD> CgFFBoxTurnXRectPositionCh3 { get; set; }

    /// <summary>
    /// X方向通光孔，记录下来每一个角度值,**需要记录**
    /// </summary>
    public double CgFFBoxTurnXLightHoleCircleAngleCh3 { get; set; }

    /// <summary>
    /// X方向通光孔，记录下来每1像素宽度相当于多少毫米,**需要记录**
    /// </summary>
    public double CgFFBoxTurnXLightHoleCircleRelationCh3 { get; set; }

    /// <summary>
    /// X方向通光孔，记录下来位移电机起始位置（单位为毫米）,**需要记录**
    /// </summary>
    public double CgFFBoxTurnXLightHoleCirclePositionCh3 { get; set; }

    /// <summary>
    /// X方向通光孔，算出来的圆形圆心坐标值,**需要记录**
    /// </summary>
    public CgPoint CgFFBoxTurnXLightHoleCircleCenterCh3 { get; set; }

    /// <summary>
    /// X方向通光孔，算出来的圆形半径,**需要记录**
    /// </summary>
    public double CgFFBoxTurnXLightHoleCircleRadiusCh3 { get; set; }

    /// <summary>
    /// Y方向转盘从120度到300度转动4个位置，记录下来每一个角度值,**需要记录**
    /// </summary>
    public List<double> CgFFBoxTurnYAngleCh3 { get; set; }

    /// <summary>
    /// Y方向转盘从120度到300度转动4个位置，记录下来每一个挡杆像素高度,**需要记录**
    /// </summary>
    public List<double> CgFFBoxTurnYWidthCh3 { get; set; }

    /// <summary>
    /// Y方向转盘从120度到300度转动4个位置，记录下来每1像素高度相当于多少毫米,**需要记录**
    /// </summary>
    public List<double> CgFFBoxTurnYMotorRelationCh3 { get; set; }

    /// <summary>
    /// Y方向转盘从120度到300度转动4个位置，记录下来位移电机起始位置（单位为毫米）,**需要记录**
    /// </summary>
    public List<double> CgFFBoxTurnYMotorPositionCh3 { get; set; }

    /// <summary>
    /// Y方向转盘从120度到300度转动4个位置，记录下来每一根挡杆矩形区域坐标,**需要记录**
    /// </summary>
    public List<RectD> CgFFBoxTurnYRectPositionCh3 { get; set; }

    /// <summary>
    /// Y方向通光孔，记录下来每一个角度值,**需要记录**
    /// </summary>
    public double CgFFBoxTurnYLightHoleCircleAngleCh3 { get; set; }

    /// <summary>
    /// Y方向通光孔，记录下来每1像素高度相当于多少毫米,**需要记录**
    /// </summary>
    public double CgFFBoxTurnYLightHoleCircleRelationCh3 { get; set; }

    /// <summary>
    /// Y方向通光孔，记录下来位移电机起始位置（单位为毫米）,**需要记录**
    /// </summary>
    public double CgFFBoxTurnYLightHoleCirclePositionCh3 { get; set; }

    /// <summary>
    /// Y方向通光孔，算出来的圆形圆心坐标值,**需要记录**
    /// </summary>
    public CgPoint CgFFBoxTurnYLightHoleCircleCenterCh3 { get; set; }

    /// <summary>
    /// Y方向通光孔，算出来的圆形半径,**需要记录**
    /// </summary>
    public double CgFFBoxTurnYLightHoleCircleRadiusCh3 { get; set; }

    /// <summary>
    /// X方向单独的推杆像素宽度,**需要记录**
    /// </summary>
    public double CgFFBoxPushXWidthCh3 { get; set; }

    /// <summary>
    /// X方向单独的推杆每1像素宽度相当于多少毫米,**需要记录**
    /// </summary>
    public double CgFFBoxPushXMotorRelationCh3 { get; set; }

    /// <summary>
    /// X方向单独的推杆起始位置,**需要记录**
    /// </summary>
    public double CgFFBoxPushXMotorPositionCh3 { get; set; }

    /// <summary>
    /// X方向单独的推杆起始矩形区域坐标,**需要记录**
    /// </summary>
    public RectD CgFFBoxPushXRectPositionCh3 { get; set; }
}

/// <summary>
/// 傅里叶PupilSideChannelSpecularBlocker校准下发Cuga参数
/// </summary>
[Serializable]
public sealed class CalibrationPupilSideChannelSpecularBlocker : CalibrationBase
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
    /// 傅里叶截图可见区域内每根杆子向下移动百分比, **需要记录**
    /// </summary> 
    public List<double> CgFFBoxMoveDownPercentListCh1 { get; set; }

    public List<double> CgFFBoxMoveDownPercentListCh2 { get; set; }
}

/// <summary>
/// 傅里叶PupilCenterChannelSpecularBlocker校准下发Cuga参数
/// </summary>
[Serializable]
public sealed class CalibrationPupilCenterChannelSpecularBlocker : CalibrationBase
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
    /// 是否选择Y方向挡杆, **NI下才需要记录**
    /// </summary>
    public bool IsSelectedY { get; set; }

    /// <summary>
    /// 傅里叶CH3通道反射光校准，Y方向挡杆位置, **NI下才需要记录**
    /// </summary> 
    public float Ch3Angle { get; set; }

    /// <summary>
    /// 傅里叶CH3通道反射光校准，Y方向挡杆位置, **NI下才需要记录**
    /// </summary> 
    public float Ch3TurnY { get; set; }

    /// <summary>
    /// 傅里叶CH3通道反射光校准，X方向推杆位置, **OI、NI下都需要记录**
    /// </summary> 
    public float Ch3Push { get; set; }
}

/// <summary>
/// 傅里叶CalibrationPupilCenterChannelFlexibleApertureV1校准下发Cuga参数
/// </summary>
[Serializable]
public sealed class CalibrationPupilCenterChannelFlexibleApertureV1 : CalibrationBase
{
    /// <summary>
    /// X转盘上4个X方向挡杆的角度.
    /// </summary>
    public List<double> CgFFBoxTurnXAngleCh3 { get; set; }

    /// <summary>
    /// X转盘上4个X方向挡杆的像素宽度.
    /// </summary>
    public List<double> CgFFBoxTurnXWidthCh3 { get; set; }

    /// <summary>
    /// X转盘上4个X方向挡杆的每像素对应的电机位移.
    /// </summary>
    public List<double> CgFFBoxTurnXMotorRelationCh3 { get; set; }

    /// <summary>
    /// X转盘上4个X方向挡杆的起始电机位置.
    /// </summary>
    public List<double> CgFFBoxTurnXMotorPositionCh3 { get; set; }

    /// <summary>
    /// X转盘上4个X方向挡杆的矩形区域坐标.
    /// </summary>
    public List<RectD> CgFFBoxTurnXRectPositionCh3 { get; set; }

    /// <summary>
    /// X方向通光孔，记录下来每一个角度值,**需要记录**
    /// </summary>
    public double CgFFBoxTurnXLightHoleCircleAngleCh3 { get; set; }

    /// <summary>
    /// X方向通光孔，记录下来每1像素宽度相当于多少毫米,**需要记录**
    /// </summary>
    public double CgFFBoxTurnXLightHoleCircleRelationCh3 { get; set; }

    /// <summary>
    /// X方向通光孔，记录下来位移电机起始位置（单位为毫米）,**需要记录**
    /// </summary>
    public double CgFFBoxTurnXLightHoleCirclePositionCh3 { get; set; }

    /// <summary>
    /// X方向通光孔，算出来的圆形圆心坐标值,**需要记录**
    /// </summary>
    public CgPoint CgFFBoxTurnXLightHoleCircleCenterCh3 { get; set; }

    /// <summary>
    /// X方向通光孔，算出来的圆形半径,**需要记录**
    /// </summary>
    public double CgFFBoxTurnXLightHoleCircleRadiusCh3 { get; set; }

    /// <summary>
    /// X转盘上4个Y方向挡杆的角度.
    /// </summary>
    public List<double> CgFFBoxTurnYAngleCh3 { get; set; }

    /// <summary>
    /// X转盘上4个Y方向挡杆的像素高度.
    /// </summary>
    public List<double> CgFFBoxTurnYWidthCh3 { get; set; }

    /// <summary>
    /// X转盘上4个Y方向挡杆的每像素对应的电机位移.
    /// </summary>
    public List<double> CgFFBoxTurnYMotorRelationCh3 { get; set; }

    /// <summary>
    /// X转盘上4个Y方向挡杆的起始电机位置.
    /// </summary>
    public List<double> CgFFBoxTurnYMotorPositionCh3 { get; set; }

    /// <summary>
    /// X转盘上4个Y方向挡杆的矩形区域坐标.
    /// </summary>
    public List<RectD> CgFFBoxTurnYRectPositionCh3 { get; set; }

    /// <summary>
    /// X转盘校准图案的角度.
    /// </summary>
    public double CgFFBoxTurnXCalibrationPatternCircleAngleCh3 { get; set; }

    /// <summary>
    /// X转盘校准图案的每像素对应的电机位移.
    /// </summary>
    public double CgFFBoxTurnXCalibrationPatternCircleRelationCh3 { get; set; }

    /// <summary>
    /// X转盘校准图案的起始电机位置.
    /// </summary>
    public double CgFFBoxTurnXCalibrationPatternCirclePositionCh3 { get; set; }

    /// <summary>
    /// X转盘校准图案的圆心坐标.
    /// </summary>
    public CgPoint CgFFBoxTurnXCalibrationPatternCircleCenterCh3 { get; set; }

    /// <summary>
    /// X转盘校准图案的半径.
    /// </summary>
    public double CgFFBoxTurnXCalibrationPatternCircleRadiusCh3 { get; set; }

    /// <summary>
    /// X反射推杆的像素宽度.
    /// </summary>
    public double CgFFBoxReflectionPushXWidthCh3 { get; set; }

    /// <summary>
    /// X反射推杆的每像素对应的电机位移.
    /// </summary>
    public double CgFFBoxReflectionPushXMotorRelationCh3 { get; set; }

    /// <summary>
    /// X反射推杆的起始电机位置.
    /// </summary>
    public double CgFFBoxReflectionPushXMotorPositionCh3 { get; set; }

    /// <summary>
    /// X反射推杆的起始矩形区域坐标.
    /// </summary>
    public RectD CgFFBoxReflectionPushXRectPositionCh3 { get; set; }

    /// <summary>
    /// X衍射推杆粗侧的像素宽度.
    /// </summary>
    public double CgFFBoxDiffractionCoarsePushXWidthCh3 { get; set; }

    /// <summary>
    /// X衍射推杆粗侧的每像素对应的电机位移.
    /// </summary>
    public double CgFFBoxDiffractionCoarsePushXMotorRelationCh3 { get; set; }

    /// <summary>
    /// X衍射推杆粗侧的起始电机位置.
    /// </summary>
    public double CgFFBoxDiffractionCoarsePushXMotorPositionCh3 { get; set; }

    /// <summary>
    /// X衍射推杆粗侧的起始矩形区域坐标.
    /// </summary>
    public RectD CgFFBoxDiffractionCoarsePushXRectPositionCh3 { get; set; }

    /// <summary>
    /// X衍射推杆细侧的像素宽度.
    /// </summary>
    public double CgFFBoxDiffractionFinePushXWidthCh3 { get; set; }

    /// <summary>
    /// X衍射推杆细侧的每像素对应的电机位移.
    /// </summary>
    public double CgFFBoxDiffractionFinePushXMotorRelationCh3 { get; set; }

    /// <summary>
    /// X衍射推杆细侧的起始电机位置.
    /// </summary>
    public double CgFFBoxDiffractionFinePushXMotorPositionCh3 { get; set; }

    /// <summary>
    /// X衍射推杆细侧的起始矩形区域坐标.
    /// </summary>
    public RectD CgFFBoxDiffractionFinePushXRectPositionCh3 { get; set; }
}

/// <summary>
/// 傅里叶PupilCenterChannelSpecularBlockerV1校准下发Cuga参数
/// </summary>
[Serializable]
public sealed class CalibrationPupilCenterChannelSpecularBlockerV1 : CalibrationBase
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
    /// 是否选择Y方向挡杆, **NI下才需要记录**
    /// </summary>
    public bool IsSelectedY { get; set; }

    /// <summary>
    /// X转盘上Y方向挡杆的角度.
    /// </summary>
    public float Ch3Angle { get; set; }

    /// <summary>
    /// X转盘上Y方向挡杆的电机位置.
    /// </summary>
    public float Ch3TurnY { get; set; }

    /// <summary>
    /// 傅里叶CH3通道反射光校准，X方向推杆位置, **OI、NI下都需要记录**
    /// </summary> 
    public float Ch3Push { get; set; }
}