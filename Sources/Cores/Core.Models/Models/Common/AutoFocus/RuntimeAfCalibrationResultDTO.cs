using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace Core.Models.Models.Common.AutoFocus;

public sealed partial class RuntimeAfCalibrationResultDTO : ObservableCacheBase, ICloneable<RuntimeAfCalibrationResultDTO>
{
    public static readonly RuntimeAfCalibrationResultDTO Default = new();

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private bool _isAFServo = true;

    [ObservableProperty]
    private double _eCSValue;

    [ObservableProperty]
    private double _motorValue;

    [ObservableProperty]
    private double _darkFieldQuality;

    [ObservableProperty]
    private string _rawImageFilePath = string.Empty;

    [ObservableProperty]
    private string _darkFieldFilePath = string.Empty;

    public RuntimeAfCalibrationResultDTO Clone() => new()
    {
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        ProductivityInformation = ProductivityInformation.Clone(),
        IsAFServo = IsAFServo,
        ECSValue = ECSValue,
        MotorValue = MotorValue,
        DarkFieldQuality = DarkFieldQuality,
        RawImageFilePath = RawImageFilePath,
        DarkFieldFilePath = DarkFieldFilePath,
        Id = Id,
        Expiration = Expiration
    };

    public object ToHtmlAnonymous() => new
    {
        IsAFServo,
        ECSValue,
        MotorValue,
        DarkFieldQuality,
        RawImageFilePath,
        Image = new HtmlImage(DarkFieldFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(false)])
    };
}