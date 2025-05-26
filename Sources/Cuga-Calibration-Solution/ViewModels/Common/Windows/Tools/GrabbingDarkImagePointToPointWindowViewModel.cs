using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using CugaCalibration.ViewModels.Chuck;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithm.Halcon.Helper;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helper.IOC.Providers;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(GrabbingDarkImagePointToPointWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class GrabbingDarkImagePointToPointWindowViewModel(
    IDialogWindowProvider dialogWindowProvider,
    ISynchronizationContextProvider contextProvider,
    ILogger<ChuckPrealignerCalibrationViewModel> logger,
    LaserViewModel laserViewModel,
    StageViewModel stageViewModel,
    IOptions<ApplicationSetting> options) : ViewModelBase
{
    [ObservableProperty]
    private int _xWidth;

    [ObservableProperty]
    private Point _startPosition;

    [ObservableProperty]
    private int _columnNumber = 1;

    [ObservableProperty]
    private double _columnCellWidth = 1;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum = OpticsMagTypeEnum.High;

    [ObservableProperty]
    private StageCoordinateSystemEnum _stageCoordinateSystemEnum = StageCoordinateSystemEnum.Bright;

    [ObservableProperty]
    private ObservableCollection<DarkFieldCropImage> _darkFieldRowScanImageList = [];

    [ObservableProperty]
    private ObservableCollection<DarkFieldCropImage> _darkFieldImageList = [];

    [RelayCommand]
    private async Task GrabbingRowImageAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                contextProvider.Send(DarkFieldRowScanImageList.Clear);
                if (dialogWindowProvider.TryShowDialog($"""
                                                        Grabbing Image Please confirm Param.
                                                        {nameof(XWidth)}: {XWidth}
                                                        {nameof(OpticsMagTypeEnum)}: {OpticsMagTypeEnum}
                                                        {nameof(StageCoordinateSystemEnum)}: {StageCoordinateSystemEnum}
                                                        """, out var dialogResultEnum, DialogButtonsEnum.OKCancel) == false || dialogResultEnum != DialogResultEnum.OK) return;

                var positionList = Enumerable.Range(0, ColumnNumber).Select(t => new Point(StartPosition.X + t * ColumnCellWidth, StartPosition.Y)).ToList();
                var cropResultList = laserViewModel.GetChuckDarkFieldRowLineScanImage(
                    positionList,
                    (false, CalibrationConstantsHelper.MainCoefficient),
                    false,
                    XWidth,
                    OpticsMagTypeEnum,
                    stageCoordinateSystemEnum: StageCoordinateSystemEnum);

                foreach (var (index, darkFieldImageDto) in cropResultList.Select((t, i) => (i, t)))
                {
                    using var _ = darkFieldImageDto;
                    var filePath = $"{options.Value.AppHomeDirectory}\\Images\\{nameof(GrabbingDarkImagePointToPointWindowViewModel)}\\{positionList[index].ToShortString()}_row_scan.jpg";
                    HalconHelper.Save(darkFieldImageDto.Image, filePath);
                    var size = HalconHelper.GetSize(darkFieldImageDto.Image);
                    contextProvider.Send(() => DarkFieldRowScanImageList.Add(new DarkFieldCropImage
                    {
                        Position = positionList[index],
                        ByteArray = darkFieldImageDto.Bytes,
                        Width = size.Width,
                        Height = size.Height,
                        FilePath = filePath,
                        DarkFieldImageList = [.. darkFieldImageDto.ProjectionYs]
                    }));
                }
            }
            catch (Exception ex)
            {
                dialogWindowProvider.ShowDialog("Get Dark Field Line Scan Image Failed, Please try again!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogError(ex, "{@Name}: Grabbing Image Failed", nameof(GrabbingDarkImagePointToPointWindowViewModel));
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GrabbingImageAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                contextProvider.Send(DarkFieldImageList.Clear);
                if (dialogWindowProvider.TryShowDialog($"""
                                                        Grabbing Image Please confirm Param.
                                                        {nameof(XWidth)}: {XWidth}
                                                        {nameof(OpticsMagTypeEnum)}: {OpticsMagTypeEnum}
                                                        {nameof(StageCoordinateSystemEnum)}: {StageCoordinateSystemEnum}
                                                        """, out var dialogResultEnum, DialogButtonsEnum.OKCancel) == false || dialogResultEnum != DialogResultEnum.OK) return;

                var positionList = Enumerable.Range(0, ColumnNumber).Select(t => new Point(StartPosition.X + t * ColumnCellWidth, StartPosition.Y)).ToList();
                foreach (var resultPosition in positionList)
                {
                    using var darkFieldImageDto = laserViewModel.GetDarkFieldLineScanImage(
                        CalChipSiteModelEnum.ChuckModel,
                        resultPosition,
                        (false, CalibrationConstantsHelper.MainCoefficient),
                        false,
                        null,
                        XWidth,
                        OpticsMagTypeEnum,
                        stageCoordinateSystemEnum: StageCoordinateSystemEnum);

                    var filePath = $"{options.Value.AppHomeDirectory}\\Images\\{nameof(GrabbingDarkImageWindowViewModel)}\\{resultPosition.ToShortString()}.jpg";
                    HalconHelper.Save(darkFieldImageDto.Image, filePath);
                    var size = HalconHelper.GetSize(darkFieldImageDto.Image);
                    var byteArray = darkFieldImageDto.Bytes;
                    var projectionYs = darkFieldImageDto.ProjectionYs;
                    contextProvider.Send(() =>
                    {
                        DarkFieldImageList.Add(new DarkFieldCropImage
                        {
                            Position = resultPosition,
                            ByteArray = byteArray,
                            Width = size.Width,
                            Height = size.Height,
                            FilePath = filePath,
                            DarkFieldImageList = [.. projectionYs]
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                dialogWindowProvider.ShowDialog("Get Dark Field Line Scan Image Failed, Please try again!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogError(ex, "{@Name}: Grabbing Image Failed", nameof(GrabbingDarkImagePointToPointWindowViewModel));
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private void GetStartPosition()
    {
        StartPosition = StageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Bright => stageViewModel.GetBrightFieldStagePosition(),
            StageCoordinateSystemEnum.Dark => stageViewModel.GetDarkFieldStagePosition(),
            StageCoordinateSystemEnum.Machine => stageViewModel.GetMachineStagePosition(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [RelayCommand]
    private async Task SavePictureAsync(DarkFieldCropImage darkFieldImage)
    {
        try
        {
            await Task.Run(() =>
            {
                var tryShowSaveFilePathDialog = dialogWindowProvider.TryShowSaveFilePathDialog(".jpg", out var saveFilePath);
                if (tryShowSaveFilePathDialog == false) return;

                System.IO.File.Copy(darkFieldImage.FilePath, saveFilePath, true);
                System.IO.File.WriteAllBytes(CalibrationConstantsHelper.ImagePathToRawImagePath(saveFilePath), darkFieldImage.ByteArray);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: Grabbing Image Failed", nameof(GrabbingDarkImagePointToPointWindowViewModel));
        }
    }

    [RelayCommand]
    private void ShowPlot(DarkFieldCropImage darkFieldImage)
    {
        try
        {
            dialogWindowProvider.ShowPlot([.. darkFieldImage.DarkFieldImageList]);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: Show Plot Failed", nameof(GrabbingDarkImagePointToPointWindowViewModel));
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseView(true);
    }

    public sealed partial class DarkFieldCropImage : ObservableObject
    {
        [ObservableProperty]
        private Point _position;

        [ObservableProperty]
        private byte[] _byteArray = [];

        [ObservableProperty]
        private double _width;

        [ObservableProperty]
        private double _height;

        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private List<double> _darkFieldImageList = [];
    }
}