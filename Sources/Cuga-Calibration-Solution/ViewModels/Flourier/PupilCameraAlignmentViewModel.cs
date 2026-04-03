using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Exceptions;
using Core.Models.Models;
using Core.Models.Models.Common.Fourier;
using Core.Models.Models.Fourier;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.WPF.Enums;
using System.IO;
using BitmapImage = Net.Utilities.Graphics.Primitives.Medias.Imaging.BitmapImage;

namespace CugaCalibration.ViewModels.Flourier;

[IOCAppService(ServiceType = typeof(PupilCameraAlignmentViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class PupilCameraAlignmentViewModel(ICalibrationAlgorithmService calibrationAlgorithmService,
    CalibrationSetting calibrationSetting, ICalibrationFourierService calibrationFlourierService,
    ICalibrationLaserService calibrationLaserService) : CalibrationViewModelBase
{
    #region 界面相关
    [ObservableProperty]
    private Point _sxPos = new();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Haze Wafer Position" },
        new() { StepName = "Get And Save CH1 Image" },
        new() { StepName = "Get And Save CH2 Image" },
        new() { StepName = "Get And Save CH3 Image" }
    ];
    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private PupilCameraAlignmentDTO _resultDto = new();

    [ObservableProperty]
    private MicroscopeCalChipDTO _microscopeCalChip = new();

    [ObservableProperty]
    private PupilCameraAlignmentCache _cache = new();

    [ObservableProperty]
    private PupilCameraAlignmentDTO _calibration = new();

    #endregion 缓存 

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false)
            return false;

        MicroscopeCalChip = CalibrationStatusService.GetCalibration<MicroscopeCalChipDTO>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<PupilCameraAlignmentCache>();
        Calibration = CacheProvider.GetOrDefault<PupilCameraAlignmentDTO>();
        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        
        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);        

        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        switch (CalibrationStepIndex)
        {
            case 1:
                Cache.IsToggleSelectRectROIDrawableCh1 = false;
                return true;

            case 2:
                Cache.IsToggleSelectRectROIDrawableCh2 = false;
                return true;

            case 3:
                Cache.IsToggleSelectRectROIDrawableCh3 = false;
                Save(ResultDto, cancellationToken);
                IsCalibrated = true;
                return true;

            default:
                Cache.IsToggleSelectRectROIDrawableCh1 = false;
                Cache.IsToggleSelectRectROIDrawableCh2 = false;
                Cache.IsToggleSelectRectROIDrawableCh3 = false;
                return true;
        }
    }

    protected override Task<bool> CancelingAsync()
    {
        return Task.FromResult(true);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
    {
        calibrationFlourierService.SetFFHome(FFCH.Ch1);
        calibrationFlourierService.SetFFHome(FFCH.Ch2);
        calibrationFlourierService.SetFFHome(FFCH.Ch3_X);
        calibrationFlourierService.SetFFHome(FFCH.Ch3_Y);   

        var HazeWaferPosition = StageViewModel.GetBrightFieldStagePosition();
        Cache.HazeWaferPosition = Cache.HazeWaferPosition != Point.Origin
          ? Cache.HazeWaferPosition
          : Guard.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition;
        Cache.HazeWaferPosition = StageViewModel.MachineToBrightFieldPosition(Cache.HazeWaferPosition);

        StageViewModel.SetAbsoluteStageTheta(0);
        AfViewModel.ToggleDarkFieldEnable(true);    
   
        SxPos = new Point(Cache.HazeWaferPosition.X, Cache.HazeWaferPosition.Y);

        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                HazeWaferPosition = Cache.HazeWaferPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        if (Cache.Ch1Image != null)
        {
            return InvokeCalibrateAsync(() =>
            {
                Cache.RectCh1Position = new Point((double)Cache.BitmapImageDrawableCh1.LastCropX * 1.0, (double)Cache.BitmapImageDrawableCh1.LastCropY * 1.0);
                Cache.Ch1ImageWidth = Cache.BitmapImageDrawableCh1.BitmapImage.Width;
                Cache.Ch1ImageHeight = Cache.BitmapImageDrawableCh1.BitmapImage.Height;
                Logger.LogHtmlInformation("ResultImage", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    RectCh1Position = new Point((double)Cache.BitmapImageDrawableCh1.LastCropX * 1.0, (double)Cache.BitmapImageDrawableCh1.LastCropY * 1.0),
                    Ch1ImageWidth = Cache.BitmapImageDrawableCh1.BitmapImage.Width,
                    Ch1ImageHeight = Cache.BitmapImageDrawableCh1.BitmapImage.Height,
                    Ch1ImageFilePath = Cache.OriginImageFilePath1,
                    //Ch2ImageFilePath = Cache.OriginImageFilePath2,
                    //Ch3ImageFilePath = Cache.OriginImageFilePath3,
                    //templateFilePath = templateFilePath,
                    HtmlTab = new HtmlTab(new
                    {
                        //TemplateImage = new HtmlImage(templateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        Ch1Image = new HtmlImage(Cache.OriginImageFilePath1)
                        //Ch2Image = new HtmlImage(OriginImageFilePath2),
                        //Ch3Image = new HtmlImage(OriginImageFilePath3)
                    })
                }), HtmlLogUniqueId.LoggingHtml());
                return true;
            });
        }
        else
        {
            DialogWindowProvider.ShowDialog("Make sure you have open image of CH1 and save channel images first，then you can goto next step!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return Task.FromResult(false);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        if (Cache.Ch2Image != null)
        {
            return InvokeCalibrateAsync(() =>
            {
                Cache.RectCh2Position = new Point((double)Cache.BitmapImageDrawableCh2.LastCropX * 1.0, (double)Cache.BitmapImageDrawableCh2.LastCropY * 1.0);
                Cache.Ch2ImageWidth = Cache.BitmapImageDrawableCh2.BitmapImage.Width;
                Cache.Ch2ImageHeight = Cache.BitmapImageDrawableCh2.BitmapImage.Height;
                Logger.LogHtmlInformation("ResultImage", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    //Ch1ImageFilePath = Cache.OriginImageFilePath1,
                    RectCh2Position = new Point((double)Cache.BitmapImageDrawableCh2.LastCropX * 1.0, (double)Cache.BitmapImageDrawableCh2.LastCropY * 1.0),
                    Ch2ImageWidth = Cache.BitmapImageDrawableCh2.BitmapImage.Width,
                    Ch2ImageHeight = Cache.BitmapImageDrawableCh2.BitmapImage.Height,
                    Ch2ImageFilePath = Cache.OriginImageFilePath2,
                    //Ch3ImageFilePath = Cache.OriginImageFilePath3,
                    //templateFilePath = templateFilePath,
                    HtmlTab = new HtmlTab(new
                    {
                        //TemplateImage = new HtmlImage(templateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        //Ch1Image = new HtmlImage(OriginImageFilePath1),
                        Ch2Image = new HtmlImage(Cache.OriginImageFilePath2)
                        //Ch3Image = new HtmlImage(OriginImageFilePath3)
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                return true;
            });
        }
        else
        {
            DialogWindowProvider.ShowDialog("Make sure you have open image of CH2 and save channel images first，then you can goto next step!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return Task.FromResult(false);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3Async(CancellationToken cancellationToken)
    {
        if (Cache.Ch3Image != null)
        {
            return InvokeCalibrateAsync(() =>
            {
                Cache.RectCh3Position = new Point((double)Cache.BitmapImageDrawableCh3.LastCropX * 1.0, (double)Cache.BitmapImageDrawableCh3.LastCropY * 1.0);
                Cache.Ch3ImageWidth = Cache.BitmapImageDrawableCh3.BitmapImage.Width;
                Cache.Ch3ImageHeight = Cache.BitmapImageDrawableCh3.BitmapImage.Height;
                Logger.LogHtmlInformation("ResultImage", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    //Ch1ImageFilePath = Cache.OriginImageFilePath1,
                    //Ch2ImageFilePath = Cache.OriginImageFilePath2,
                    RectCh3Position = new Point((double)Cache.BitmapImageDrawableCh3.LastCropX * 1.0, (double)Cache.BitmapImageDrawableCh3.LastCropY * 1.0),
                    Ch3ImageWidth = Cache.BitmapImageDrawableCh3.BitmapImage.Width,
                    Ch3ImageHeight = Cache.BitmapImageDrawableCh3.BitmapImage.Height,
                    Ch3ImageFilePath = Cache.OriginImageFilePath3,
                    //templateFilePath = templateFilePath,
                    HtmlTab = new HtmlTab(new
                    {
                        //TemplateImage = new HtmlImage(templateFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        //Ch1Image = new HtmlImage(Cache.OriginImageFilePath1),
                        //Ch2Image = new HtmlImage(Cache.OriginImageFilePath2),
                        Ch3Image = new HtmlImage(Cache.OriginImageFilePath3)
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                ResultDto.RectCh1Position = Cache.RectCh1Position;
                ResultDto.Ch1ImageWidth = Cache.Ch1ImageWidth;
                ResultDto.Ch1ImageHeight = Cache.Ch1ImageHeight;
                ResultDto.RectCh2Position = Cache.RectCh2Position;
                ResultDto.Ch2ImageWidth = Cache.Ch2ImageWidth;
                ResultDto.Ch2ImageHeight = Cache.Ch2ImageHeight;
                ResultDto.RectCh3Position = Cache.RectCh3Position;
                ResultDto.Ch3ImageWidth = Cache.Ch3ImageWidth;
                ResultDto.Ch3ImageHeight = Cache.Ch3ImageHeight;

                return true;
            });
        }
        else
        {
            DialogWindowProvider.ShowDialog("Make sure you have open image of CH3 and save channel images first，then you can goto next step!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return Task.FromResult(false);
        }
    }

    [RelayCommand]
    private async Task OpenImageFileCH1Async()
    {          
        var ret = calibrationFlourierService.GetFFReviewImgForTrigger(0, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
        if (ret.IsSuccess == false)
        {
            throw new CugaException(ret.ErrorMsg);
        }
        else
        {
            var originPicture0 = ret.Anything;
            if (originPicture0 == null)
            {
                // 处理错误或返回
                return;
            }
            Cache.BitmapImageDrawableCh1 = new BitmapImageDrawable();

            var bitmap = BytesToBitmapImage(originPicture0);
            if (bitmap == null)
                return;
            Cache.BitmapImageDrawableCh1.BitmapImage = bitmap;
        }
    }

    [RelayCommand]
    private async Task SaveImageFileCH1Async()
    {
        Cache.OriginImageFilePath1 = Path.Combine(ImageFileDirectory, "CH1", $"{Guid.NewGuid():N}.jpg");
        //var templateFilePath = $"{originImageFilePath}_Template";
        Cache.BitmapImageDrawableCh1.BitmapImage.Save(Cache.OriginImageFilePath1);

        Cache.Ch1Image = Cache.BitmapImageDrawableCh1.BitmapImage;
    }

    [RelayCommand]
    private async Task OpenImageFileCH2Async()
    {
        var ret = calibrationFlourierService.GetFFReviewImgForTrigger(1, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
        if (ret.IsSuccess == false)
        {
            throw new CugaException(ret.ErrorMsg);
        }
        else
        {
            //BitmapImageDrawableCh2 = null;
            var originPicture0 = ret.Anything;
            if (originPicture0 == null)
            {
                // 处理错误或返回
                return;
            }
            Cache.BitmapImageDrawableCh2 = new BitmapImageDrawable();

            var bitmap = BytesToBitmapImage(originPicture0);
            if (bitmap == null)
                return;
            Cache.BitmapImageDrawableCh2.BitmapImage = bitmap;
        }
    }

    [RelayCommand]
    private async Task SaveImageFileCH2Async()
    {
        Cache.OriginImageFilePath2 = Path.Combine(ImageFileDirectory, "CH2", $"{Guid.NewGuid():N}.jpg");
        //var templateFilePath = $"{originImageFilePath}_Template";
        Cache.BitmapImageDrawableCh2.BitmapImage.Save(Cache.OriginImageFilePath2);

        Cache.Ch2Image = Cache.BitmapImageDrawableCh2.BitmapImage;
    }

    [RelayCommand]
    private async Task OpenImageFileCH3Async()
    {
        var ret = calibrationFlourierService.GetFFReviewImgForTrigger(2, Cache.ProductivityInformation, Cache.LaserLightInformation.Level, SxPos, 100);
        if (ret.IsSuccess == false)
        {
            throw new CugaException(ret.ErrorMsg);
        }
        else
        {
            var originPicture0 = ret.Anything;
            if (originPicture0 == null)
            {
                // 处理错误或返回
                return;
            }
            Cache.BitmapImageDrawableCh3 = new BitmapImageDrawable();

            var bitmap = BytesToBitmapImage(originPicture0);
            if (bitmap == null)
                return;
            Cache.BitmapImageDrawableCh3.BitmapImage = bitmap;
        }
    }

    [RelayCommand]
    private async Task SaveImageFileCH3Async()
    {
        Cache.OriginImageFilePath3 = Path.Combine(ImageFileDirectory, "CH3", $"{Guid.NewGuid():N}.jpg");
        //var templateFilePath = $"{originImageFilePath}_Template";
        Cache.BitmapImageDrawableCh3.BitmapImage.Save(Cache.OriginImageFilePath3);

        Cache.Ch3Image = Cache.BitmapImageDrawableCh3.BitmapImage;
    }

    private bool Save(PupilCameraAlignmentDTO itemDto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        Calibration = itemDto.Clone();

        CacheProvider.Set(Calibration, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    public static BitmapImage BytesToBitmapImage(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return null;
        // Net.Utilities.Graphics.Primitives.Medias.Imaging.BitmapImage
        // 使用接受字节数组的构造函数（反编译源码显示有此构造函数）
        return new BitmapImage(bytes, isCopy: true);
    }
}
