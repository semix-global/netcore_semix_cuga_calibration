using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models;

namespace Core.Models.Models.Laser.Rtfc
{
    public sealed partial class RtfcCache : CalibrationCacheBase
    {
        /// <summary>
        /// 入射角（°）
        /// </summary>
        [ObservableProperty]
        private double _obliqueAngle = 53;

        [ObservableProperty]
        private double _autoFocusEcs;

        [ObservableProperty]
        private double _ideaDarkFieldEcs;

        [ObservableProperty]
        private Point _highSiteFindPosition;

        [ObservableProperty]
        private Point _ideaDarkFieldMachinePosition;

        [ObservableProperty]
        private double _findFocusMin;

        [ObservableProperty]
        private double _findFocusMax;

        [ObservableProperty]
        private double _findFocusInterval;

        [ObservableProperty]
        private double _qualityThreshold;

        [ObservableProperty]
        private double _offsetThreshold;

        [ObservableProperty]
        private double _afEcsRelation = 30;
    }
}
