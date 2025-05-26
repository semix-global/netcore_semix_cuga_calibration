using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Setting;
using Net.Utilities.Models;

namespace Core.Models.Models.Laser.Rtfc
{
    public sealed partial class RtfcCache : CalibrationCacheBase
    {
        [ObservableProperty]
        private MicroscopeMagnificationEnum _lowMicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification5X;

        [ObservableProperty]
        private MicroscopeMagnificationEnum _highMicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification50X;

        [ObservableProperty]
        private OpticsMagTypeEnum _opticsMagTypeEnum = OpticsMagTypeEnum.High;

        [ObservableProperty]
        private StageSpeedEnum _stageSpeedEnum = StageSpeedEnum.Low;

        [ObservableProperty]
        private SettingDarkFieldAutoFocusParam _settingDarkFieldAutoFocusParam = new();

        [ObservableProperty]
        private Point _lowSiteFindPosition;

        [ObservableProperty]
        private Point _highSiteFindPosition;

        [ObservableProperty]
        private Point _darkFieldFindPosition;

        [ObservableProperty]
        private Point _machinePosition;

        /// <summary>
        /// 入射角（°）
        /// </summary>
        [ObservableProperty]
        private double _obliqueAngle = 53;

        [ObservableProperty]
        private double _autoFocusEcs;

        [ObservableProperty]
        private double _findFocusMin;

        [ObservableProperty]
        private double _findFocusMax;

        [ObservableProperty]
        private double _findFocusInterval;

        [ObservableProperty]
        private string _lowSiteTemplateFilePath = string.Empty;

        [ObservableProperty]
        private string _lowSiteTemplateImageFilePath = string.Empty;

        [ObservableProperty]
        private string _highSiteTemplateFilePath = string.Empty;

        [ObservableProperty]
        private string _highSiteTemplateImageFilePath = string.Empty;

        [ObservableProperty]
        private string _darkFiledTemplateFilePath = string.Empty;

        [ObservableProperty]
        private string _darkFiledTemplateImageFilePath = string.Empty;

        [ObservableProperty]
        private string _darkFiledImageFilePath = string.Empty;

        [ObservableProperty]
        private double _qualityThreshold;

        [ObservableProperty]
        private double _offsetThreshold;

        [ObservableProperty]
        private double _afEcsRelation = 30;
    }
}