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
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.ChuckModel;

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial bool IsAFServo { get; set; } = true;

    [ObservableProperty]
    public partial double ECSValue { get; set; }

    [ObservableProperty]
    public partial double MotorValue { get; set; }

    [ObservableProperty]
    public partial double DarkFieldQuality { get; set; }

    [ObservableProperty]
    public partial string RawImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DarkFieldFilePath { get; set; } = string.Empty;

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