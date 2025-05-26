using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

namespace Core.Models.Models.Laser.Rtfc
{
    public sealed partial class RtfcDto : CalibrationDtoBase, ICloneable<RtfcDto>
    {
        [ObservableProperty]
        private int _index;

        [ObservableProperty]
        private Point _brightFieldFindPosition;

        [ObservableProperty]
        private Point _darkFieldFindPosition;

        [ObservableProperty]
        private double _ecsValue;

        [ObservableProperty]
        private double _quality;

        [ObservableProperty]
        private string _darkFieldImageFilePath = string.Empty;

        [ObservableProperty]
        private string _darkFieldOriginImageFilePath = string.Empty;

        [ObservableProperty]
        private Point _darkFieldMatchOffset;

        /// <summary>
        /// 照明轴向修正量
        /// </summary>
        [ObservableProperty]
        private double _illuminationFocusOffset;

        /// <summary>
        /// chuck ecs修正量
        /// </summary>
        [ObservableProperty]
        private double _deltaEcs;

        /// <summary>
        /// Nsc offset
        /// </summary>
        [ObservableProperty]
        private double _deltaNsc;

        /// <summary>
        ///  af电机修正值
        /// </summary>
        [ObservableProperty]
        private double _afMotor;

        public RtfcDto Clone() => new()
        {
            Index = Index,
            BrightFieldFindPosition = BrightFieldFindPosition,
            DarkFieldFindPosition = DarkFieldFindPosition,
            EcsValue = EcsValue,
            Quality = Quality,
            DarkFieldImageFilePath = DarkFieldImageFilePath,
            DarkFieldOriginImageFilePath = DarkFieldOriginImageFilePath,
            DarkFieldMatchOffset = DarkFieldMatchOffset,
            IlluminationFocusOffset = IlluminationFocusOffset,
            DeltaEcs = DeltaEcs,
            DeltaNsc = DeltaNsc,
            AfMotor = AfMotor,
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredSelfCheck = IsRequiredSelfCheck,
            Id = Id,
            Expiration = Expiration
        };
    }
}