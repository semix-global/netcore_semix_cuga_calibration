using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Microscope.Enums;
using Cuga.Data.DataStruct.Optics;
using Cuga.Data.DataStruct.Stage;
using Core.Wcf.Models.Laser;
using System.Collections.ObjectModel;
using Cuga.Data.DataStruct.DTO.Recipe;





#if NETFRAMEWORK
using Cuga.Data.DataStruct.PMT;
#endif


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
    public Rectangle RectCh1 { get; set; }

    /// <summary>
    /// 通道1截图矩形区域坐标值, **需要记录**
    /// </summary> 
    public Rectangle RectCh2 { get; set; }

    /// <summary>
    /// 通道1截图矩形区域坐标值, **需要记录**
    /// </summary> 
    public Rectangle RectCh3 { get; set; }   
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
    /// 每根杆子像素高度和真实高度对应比例换算，按照百分比, **需要记录**
    /// </summary> 
    public List<int> CgFFBoxHeightRelationPercentListCh1 { get; set; }
    public List<int> CgFFBoxHeightRelationPercentListCh2 { get; set; }

    public List<Rectangle> CurrentImageRectListFirstCh1 { get; set; }

    public List<Rectangle> CurrentImageRectListFirstCh2 { get; set; }

    public double CgFFBoxAllRodsBeginPercentCh1 { get; set; }

    public double CgFFBoxAllRodsBeginPercentCh2 { get; set; }
}

/// <summary>
/// 傅里叶CalibrationPupilCenterChannelFlexibleAperture校准下发Cuga参数
/// </summary>
[Serializable]
public sealed class CalibrationPupilCenterChannelFlexibleAperture : CalibrationBase
{    
    public List<double> CgFFBoxTurnXAngleCh3 { get; set; }

    public List<double> CgFFBoxTurnXWidthCh3 { get; set; }

    public List<double> CgFFBoxTurnXMotorRelationCH3 { get; set; }

    public List<double> CgFFBoxTurnXMotorPositionCH3 { get; set; }

    public List<Rectangle> CgFFBoxTurnXRectPositionCH3 { get; set; }

    public Point CgFFBoxTurnXLightHoleCircleCenterCh3 { get; set; }

    public double CgFFBoxTurnXLightHoleCircleRadiusCh3 { get; set; }

    public List<double> CgFFBoxTurnYAngleCh3 { get; set; }

    public List<double> CgFFBoxTurnYWidthCh3 { get; set; }

    public List<double> CgFFBoxTurnYMotorRelationCH3 { get; set; }

    public List<double> CgFFBoxTurnYMotorPositionCH3 { get; set; }

    public List<Rectangle> CgFFBoxTurnYRectPositionCH3 { get; set; }

    public Point CgFFBoxTurnYLightHoleCircleCenterCh3 { get; set; }

    public double CgFFBoxTurnYLightHoleCircleRadiusCh3 { get; set; }

    public double CgFFBoxPushXWidthCh3 { get; set; }

    public double CgFFBoxPushXMotorRelationCH3 { get; set; }

    public double CgFFBoxPushXMotorPositionCH3 { get; set; }

    public Rectangle CgFFBoxPushXRectPositionCH3 { get; set; }
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

