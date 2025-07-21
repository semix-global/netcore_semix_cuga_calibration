using Cuga.Data.DataStruct.Microscope.Enums;
using Cuga.Data.DataStruct.Stage;
using System;
using System.ComponentModel;

namespace Core.Wcf.Models.Microscope
{
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
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, Chuck暗场Af最佳Ecs
        /// </summary>
        public double ChuckAfEcsValue { get; set; }

        /// <summary>
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, Chuck暗场Af电机值
        /// </summary>
        public double ChuckAfMotorValue { get; set; }

        /// <summary>
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, DSW明场中心的机械位置(绝对位置), **需要下发AF硬件**
        /// </summary>
        public CgPoint DswBrightFieldMachinePosition { get; set; }

        /// <summary>
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, DSW暗场中心的机械位置(绝对位置), **需要下发AF硬件**
        /// </summary>
        public CgPoint DswDarkFieldMachinePosition { get; set; }

        /// <summary>
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, DSW最佳清晰高度(绝对ECS), **需要下发AF硬件**
        /// </summary>
        public double DswEcsValue { get; set; }

        /// <summary>
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, Dsw暗场Af最佳Ecs
        /// </summary>
        public double DswAfEcsValue { get; set; }

        /// <summary>
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, Dsw暗场Af电机值
        /// </summary>
        public double DswAfMotorValue { get; set; }

        /// <summary>
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, Undefined明场中心的机械位置(绝对位置), **需要下发AF硬件**
        /// </summary>
        public CgPoint UndefinedBrightFieldMachinePosition { get; set; }

        /// <summary>
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, Undefined暗场中心的机械位置(绝对位置), **需要下发AF硬件**
        /// </summary>
        public CgPoint UndefinedDarkFieldMachinePosition { get; set; }

        /// <summary>
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, Undefined最佳清晰高度(绝对ECS), **需要下发AF硬件**
        /// </summary>
        public double UndefinedEcsValue { get; set; }

        /// <summary>
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, Haze明场中心的机械位置(绝对位置), **需要下发AF硬件**
        /// </summary>
        public CgPoint HazeBrightFieldMachinePosition { get; set; }

        /// <summary>
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, Haze暗场中心的机械位置(绝对位置), **需要下发AF硬件**
        /// </summary>
        public CgPoint HazeDarkFieldMachinePosition { get; set; }

        /// <summary>
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, Haze最佳清晰高度(绝对ECS), **需要下发AF硬件**
        /// </summary>
        public double HazeEcsValue { get; set; }

        /// <summary>
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, Haze暗场Af最佳Ecs
        /// </summary>
        public double HazeAfEcsValue { get; set; }

        /// <summary>
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, Haze暗场Af电机值
        /// </summary>
        public double HazeAfMotorValue { get; set; }

        /// <summary>
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, ShinyWafer明场中心的机械位置(绝对位置), **需要下发AF硬件**
        /// </summary>
        public CgPoint ShinyWaferBrightFieldMachinePosition { get; set; }

        /// <summary>
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, ShinyWafer暗场中心的机械位置(绝对位置), **需要下发AF硬件**
        /// </summary>
        public CgPoint ShinyWaferDarkFieldMachinePosition { get; set; }

        /// <summary>
        /// 基于<see cref="CgMicroscopeLens"/>倍镜下做的校准, ShinyWafer最佳清晰高度(绝对ECS), **需要下发AF硬件**
        /// </summary>
        public double ShinyWaferEcsValue { get; set; }
    }

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
}