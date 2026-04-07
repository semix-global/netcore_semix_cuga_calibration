using System;
using System.Collections.Generic;
using Cuga.Data.DataStruct.Stage;
using Cuga.Data.DataStruct.DTO.Recipe;

namespace Core.Wcf.Models.Fourier;

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
    /// 每根杆子像素高度和真实高度对应比例换算，按照百分比, 每1%相当于多少像素,**需要记录**
    /// </summary> 
    public List<int> CgFFBoxHeightRelationPercentListCh1 { get; set; }
    public List<int> CgFFBoxHeightRelationPercentListCh2 { get; set; }

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
    public List<RectD> CgFFBoxTurnXRectPositionCH3 { get; set; }

    /// <summary>
    /// 垂直方向通光孔算出来的圆形圆心坐标值,**需要记录**
    /// </summary> 
    public CgPoint CgFFBoxTurnXLightHoleCircleCenterCh3 { get; set; }

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
    public List<RectD> CgFFBoxTurnYRectPositionCH3 { get; set; }

    /// <summary>
    /// 水平方向通光孔算出来的圆形圆心坐标值,**需要记录**
    /// </summary> 
    public CgPoint CgFFBoxTurnYLightHoleCircleCenterCh3 { get; set; }

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
    public RectD CgFFBoxPushXRectPositionCH3 { get; set; }
}

/// <summary>
/// 傅里叶PupilSideChannelSpecularBlocker校准下发Cuga参数
/// </summary>
[Serializable]
public sealed class CalibrationPupilSideChannelSpecularBlocker : CalibrationBase
{
    /// <summary>
    /// 傅里叶截图可见区域内开始移动的第一根杆子像素坐标, **不需要记录**
    /// </summary> 
    public CgPoint CgFFBoxBeginPositionCh1 { get; set; }
    public CgPoint CgFFBoxBeginPositionCh2 { get; set; }

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

