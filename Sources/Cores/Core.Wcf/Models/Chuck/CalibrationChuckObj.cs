using Cuga.Data.DataStruct.Microscope.Enums;
using Cuga.Data.DataStruct.Optics;
using Cuga.Data.DataStruct.Stage;
using System;
using System.ComponentModel;
using System.Linq;

#if NET
using ADSSpeedEnum = Cuga.Data.DataStruct.DTO.Swath.CgSpeedLevelType;
#else
using Cuga.Data.DataStruct.ADS;

#endif

namespace Core.Wcf.Models.Chuck
{
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
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, 校准后EFEM上下料时Chuck的起始旋转角度, **Cuga内部使用**
        /// </summary>
        public double NewEfemLoadWaferChuckAngle { get; set; }
    }

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
                        Location = ErrorMatrix.ElementAtOrDefault(row)?.ElementAtOrDefault(column) ?? CgPoint.Empty,
                    }).ToList()
                }).ToList()
            };
        }
    }
}