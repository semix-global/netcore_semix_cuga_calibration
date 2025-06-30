using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Setting;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.Rtfc
{
    public sealed partial class RtfcDto : CalibrationDtoBase, ICloneable<RtfcDto>
    {
        [ObservableProperty]
        private OpticsMagTypeEnum _opticsMagTypeEnum = OpticsMagTypeEnum.High;

        [ObservableProperty]
        private int _index;

        [ObservableProperty]
        private SettingDarkFieldAutoFocusParam _darkFieldAutoFocusParam = new();

        [ObservableProperty]
        private Point _brightFieldFindPosition;

        [ObservableProperty]
        private Point _darkFieldFindPosition;

        [ObservableProperty]
        private double _ecsValue;

        [ObservableProperty]
        private double _nscValue;

        [ObservableProperty]
        private double _autoFocusEcs;

        [ObservableProperty]
        private double _autoFocusNsc;

        [ObservableProperty]
        private double _idealEcs;

        [ObservableProperty]
        private double _idealEcsNsc;

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
        private double _lightAxisOffset;

        /// <summary>
        /// chuck ecs修正量
        /// </summary>
        [ObservableProperty]
        private double _deltaEcs;

        /// <summary>
        ///  af电机修正值
        /// </summary>
        [ObservableProperty]
        private double _afMotorOffset;

        /// <summary>
        /// Nsc offset
        /// </summary>
        [ObservableProperty]
        private double _deltaNsc;

        public double DfOffset => EcsValue - AutoFocusEcs;
        public double AfOffset => AutoFocusEcs - IdealEcs;

        public RtfcDto Clone() => new()
        {
            Index = Index,
            DarkFieldAutoFocusParam = DarkFieldAutoFocusParam.Clone(),
            BrightFieldFindPosition = BrightFieldFindPosition,
            DarkFieldFindPosition = DarkFieldFindPosition,
            EcsValue = EcsValue,
            NscValue = NscValue,
            AutoFocusEcs = AutoFocusEcs,
            AutoFocusNsc = AutoFocusNsc,
            IdealEcs = IdealEcs,
            IdealEcsNsc = IdealEcsNsc,
            Quality = Quality,
            DarkFieldImageFilePath = DarkFieldImageFilePath,
            DarkFieldOriginImageFilePath = DarkFieldOriginImageFilePath,
            DarkFieldMatchOffset = DarkFieldMatchOffset,
            LightAxisOffset = LightAxisOffset,
            DeltaEcs = DeltaEcs,
            DeltaNsc = DeltaNsc,
            AfMotorOffset = AfMotorOffset,
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredSelfCheck = IsRequiredSelfCheck,
            Id = Id,
            Expiration = Expiration
        };
    }
}