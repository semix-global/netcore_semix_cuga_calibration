using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Models.Models.Common.Cookies;

public sealed partial class CalibrationViewModelEntry(
    Type viewModelType,
    Type cacheType,
    Type dtoType,
    Type? adaptToCUGAType,
    bool isArray,
    ICalibrationViewModelCookie<CalibrationCacheBase, CalibrationDTOBase> cookie) : ObservableObject
{
    public static readonly CalibrationViewModelEntry Default = new(typeof(Empty), typeof(Empty), typeof(Empty), typeof(Empty), false, new Empty());

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    public Type ViewModelType { get; } = viewModelType;

    public Type CacheType { get; } = cacheType;

    public Type DTOType { get; } = dtoType;

    public Type? AdaptToCUGAType { get; } = adaptToCUGAType;

    public bool IsArray { get; } = isArray;

    public ICalibrationViewModelCookie<CalibrationCacheBase, CalibrationDTOBase> Cookie { get; } = cookie;

    public CalibrationViewModelStatus Status { get; } = new();

    private sealed class Empty : ICalibrationViewModelCookie<CalibrationCacheBase, CalibrationDTOBase>
    {
        public bool IsArray { get; } = false;

        public CalibrationCacheBase Cache => CommunityToolkit.Diagnostics.ThrowHelper.ThrowNotSupportedException<CalibrationCacheBase>();

        public CalibrationDTOBase Calibration => CommunityToolkit.Diagnostics.ThrowHelper.ThrowNotSupportedException<CalibrationDTOBase>();

        public CalibrationDTOBase[] Calibrations => CommunityToolkit.Diagnostics.ThrowHelper.ThrowNotSupportedException<CalibrationDTOBase[]>();
    }
}