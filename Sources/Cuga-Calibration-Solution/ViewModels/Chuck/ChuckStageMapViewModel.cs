using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Algorithm.Extensions;
using Core.Algorithm.Models.TemplateMatch;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.Microscope.CalChip;
using CugaCalibration.ViewModels.Common.Windows.Tools.Stage;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;

namespace CugaCalibration.ViewModels.Chuck;

[IOCAppService(ServiceType = typeof(ChuckStageMapViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckStageMapViewModel(StageMapCommands stageMapCommands) : CalibrationViewModelBase<ChuckStageMapCache>
{
    #region 属性

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "BF Param" },
        new() { StepName = "BF Alignment" },
        new() { StepName = "Generate BF StageMap" },
        new() { StepName = "Scan BF StageMap" },
        new() { StepName = "DF Param" },
        new() { StepName = "DF Alignment" },
        new() { StepName = "Generate DF StageMap" },
        new() { StepName = "Scan DF StageMap" },
        new() { StepName = "Repeat Scan DF StageMap" },
        new() { StepName = "Merge" }
    ];

    public StageMapCommands StageMapCommands { get; } = stageMapCommands;

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial ChuckStageMapDTO CalibratingItem { get; set; } = new();

    #endregion Calibrate

    [ObservableProperty]
    public partial ChuckStageMapDTO Review { get; set; } = new();

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial ChuckStageMapCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial ChuckStageMapDTO Calibration { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<ChuckStageMapCache>(cancellationToken);
        Calibration = ApplicationCookieService.GetCalibration<ChuckStageMapDTO>(cancellationToken);

        StageMapCommands.Logger = Logger;
        StageMapCommands.ImageFileDirectory = ImageFileDirectory;
        StageMapCommands.TemplateFileDirectory = TemplateFileDirectory;
        StageMapCommands.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
        StageMapCommands.MicroscopeCalChip = MicroscopeCalChip;
        StageMapCommands.Cache = Cache;
        StageMapCommands.ChuckStageMap = CalibratingItem;

        UpdateEntryStatus(Calibration, cancellationToken);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Review = Calibration;

        StageMapCommands.ChuckStageMap = Review;

        Review.BFStageMap.Refresh();
        Review.DFStageMap.Refresh();
        foreach (var stageMap in Review.DFRepeatStageMaps) stageMap.Refresh();
        Review.ExpandStageMap.Refresh();
        Review.BFVerifyStageMap.Refresh();
        Review.DFVerifyStageMap.Refresh();

        return Review.IsCalibrated;
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Guard.IsFalse(StageMapCommands.AddStageMapTemplateCommand.IsRunning
                      || StageMapCommands.GotoStageMapDocumentSelectedItemPositionCommand.IsRunning);

        switch (CalibrationStepIndex)
        {
            case 0:
                return true;

            case 1:
                return true;

            case 2:
                return true;

            case 3:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.ChuckStageMapCacheBFItem.StageMapTemplates.FirstOrDefault()?.FindBFMachinePosition ?? MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)), CalChipSiteModelEnum.ChuckModel);

                return true;

            case 4:
                StageMapCommands.IsDark = false;

                return true;

            case 5:
                return true;

            case 6:
                return true;

            case 7:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.ChuckStageMapCacheDFItem.StageMapTemplates.FirstOrDefault()?.FindBFMachinePosition ?? MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)), CalChipSiteModelEnum.ChuckModel);

                return true;

            case 8:
                return true;

            case 9:
                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Guard.IsFalse(StageMapCommands.AddStageMapTemplateCommand.IsRunning
                      || StageMapCommands.GotoStageMapDocumentSelectedItemPositionCommand.IsRunning);

        switch (CalibrationStepIndex)
        {
            case 0:
                StageMapCommands.IsDark = false;

                CalibratingItem = new ChuckStageMapDTO
                {
                    MicroscopeLensInformation = Cache.MicroscopeLensInformation
                };
                StageMapCommands.ChuckStageMap = CalibratingItem;

                return true;

            case 1:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.ChuckStageMapCacheBFItem.StageMapTemplates.FirstOrDefault()?.FindBFMachinePosition ?? MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)), CalChipSiteModelEnum.ChuckModel);

                return true;

            case 2:
                return true;

            case 3:
                return true;

            case 4:
                StageMapCommands.IsDark = true;

                return true;

            case 5:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.ChuckStageMapCacheDFItem.StageMapTemplates.FirstOrDefault()?.FindBFMachinePosition ?? MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)), CalChipSiteModelEnum.ChuckModel);

                return true;

            case 6:
                return true;

            case 7:
                return true;

            case 8:
                return true;

            case 9:
                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

                return true;

            default:
                return false;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            StageMapCommands.IsDark = false;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation,
                StageMapCommands.IsDark
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.MicroscopeLensInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.MicroscopeLensInformation,
                StageMapCommands.IsDark
            }), HtmlLogUniqueId.LoggingHtml());

            await StageMapCommands.AlignmentThenApplyTemplatesAsync(HtmlLogUniqueId, cancellationToken).ConfigureAwait(false);

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step2Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.MicroscopeLensInformation,
                BFParam = new HtmlQuote(new
                {
                    TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheBFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                    Cache.ChuckStageMapCacheBFItem.WaferRadius,
                    Cache.ChuckStageMapCacheBFItem.DiePitchWidth,
                    Cache.ChuckStageMapCacheBFItem.DiePitchHeight,
                    Table = new HtmlTable(
                    [
                        .. Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Index().Select(t => new
                        {
                            t.Index,
                            t.Item.FindBFMachinePosition,
                            t.Item.TemplateROI,
                            t.Item.TemplateFilePath,
                            HtmlTab = new HtmlTab(new
                            {
                                ResultImage = new HtmlImage(t.Item.OriginImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(t.Item.TemplateROI)]),
                                TemplateImage = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                            })
                        })
                    ])
                }),
                StageMapCommands.IsDark
            }), HtmlLogUniqueId.LoggingHtml());

            await StageMapCommands.GenerateStageMapDocumentAsync(HtmlLogUniqueId, cancellationToken).ConfigureAwait(false);

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step3Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () =>
    {
        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            Cache.MicroscopeLensInformation,
            BFParam = new HtmlQuote(new
            {
                TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheBFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                Cache.ChuckStageMapCacheBFItem.WaferRadius,
                Cache.ChuckStageMapCacheBFItem.DiePitchWidth,
                Cache.ChuckStageMapCacheBFItem.DiePitchHeight,
                Table = new HtmlTable(
                [
                    .. Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Index().Select(t => new
                    {
                        t.Index,
                        t.Item.FindBFMachinePosition,
                        t.Item.TemplateROI,
                        t.Item.TemplateFilePath,
                        HtmlTab = new HtmlTab(new
                        {
                            ResultImage = new HtmlImage(t.Item.OriginImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(t.Item.TemplateROI)]),
                            TemplateImage = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    })
                ])
            }),
            StageMapCommands.IsDark
        }), HtmlLogUniqueId.LoggingHtml());

        await StageMapCommands.CalibrateAsync(false, HtmlLogUniqueId, cancellationToken).ConfigureAwait(false);

        return true;
    });

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step4Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            CalibratingItem.DFProductivityInformation = Cache.ChuckStageMapCacheDFItem.ProductivityInformation;
            StageMapCommands.IsDark = true;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation,
                DFParam = new HtmlQuote(new
                {
                    Cache.ChuckStageMapCacheDFItem.ProductivityInformation,
                    Cache.ChuckStageMapCacheDFItem.LaserLightInformation,
                    Cache.ChuckStageMapCacheDFItem.CIBInformation,
                    OpticsConfiguration = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.OpticsConfiguration.ToHtmlAnonymous()),
                    CIBConfiguration = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.CIBConfiguration.ToHtmlAnonymous())
                }),
                StageMapCommands.IsDark
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.ProductivityInformations.Contains(Cache.ChuckStageMapCacheDFItem.ProductivityInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.ChuckStageMapCacheDFItem.LaserLightInformation)
                   && ApplicationCookie.CIBInformations.Contains(Cache.ChuckStageMapCacheDFItem.CIBInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step5Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.MicroscopeLensInformation,
                DFParam = new HtmlQuote(new
                {
                    Cache.ChuckStageMapCacheDFItem.ProductivityInformation,
                    Cache.ChuckStageMapCacheDFItem.LaserLightInformation,
                    Cache.ChuckStageMapCacheDFItem.CIBInformation,
                    OpticsConfiguration = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.OpticsConfiguration.ToHtmlAnonymous()),
                    CIBConfiguration = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.CIBConfiguration.ToHtmlAnonymous())
                }),
                StageMapCommands.IsDark
            }), HtmlLogUniqueId.LoggingHtml());

            await StageMapCommands.AlignmentThenApplyTemplatesAsync(HtmlLogUniqueId, cancellationToken).ConfigureAwait(false);

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step6Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.MicroscopeLensInformation,
                DFParam = new HtmlQuote(new
                {
                    Cache.ChuckStageMapCacheDFItem.ProductivityInformation,
                    Cache.ChuckStageMapCacheDFItem.LaserLightInformation,
                    Cache.ChuckStageMapCacheDFItem.CIBInformation,
                    OpticsConfiguration = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.OpticsConfiguration.ToHtmlAnonymous()),
                    CIBConfiguration = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.CIBConfiguration.ToHtmlAnonymous()),
                    TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                    Cache.ChuckStageMapCacheDFItem.WaferRadius,
                    Cache.ChuckStageMapCacheDFItem.DiePitchWidth,
                    Cache.ChuckStageMapCacheDFItem.DiePitchHeight,
                    Cache.ChuckStageMapCacheDFItem.ImageWidth,
                    Table = new HtmlTable(
                    [
                        .. Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Index().Select(t => new
                        {
                            t.Index,
                            t.Item.FindBFMachinePosition,
                            t.Item.TemplateROI,
                            t.Item.TemplateFilePath,
                            HtmlTab = new HtmlTab(new
                            {
                                ResultImage = new HtmlImage(t.Item.OriginImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(t.Item.TemplateROI)]),
                                TemplateImage = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                            })
                        })
                    ])
                }),
                StageMapCommands.IsDark
            }), HtmlLogUniqueId.LoggingHtml());

            await StageMapCommands.GenerateStageMapDocumentAsync(HtmlLogUniqueId, cancellationToken).ConfigureAwait(false);

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step7Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () =>
    {
        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            Cache.MicroscopeLensInformation,
            DFParam = new HtmlQuote(new
            {
                Cache.ChuckStageMapCacheDFItem.ProductivityInformation,
                Cache.ChuckStageMapCacheDFItem.LaserLightInformation,
                Cache.ChuckStageMapCacheDFItem.CIBInformation,
                OpticsConfiguration = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.CIBConfiguration.ToHtmlAnonymous()),
                TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                Cache.ChuckStageMapCacheDFItem.WaferRadius,
                Cache.ChuckStageMapCacheDFItem.DiePitchWidth,
                Cache.ChuckStageMapCacheDFItem.DiePitchHeight,
                Cache.ChuckStageMapCacheDFItem.ImageWidth,
                Table = new HtmlTable(
                [
                    .. Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Index().Select(t => new
                    {
                        t.Index,
                        t.Item.FindBFMachinePosition,
                        t.Item.TemplateROI,
                        t.Item.TemplateFilePath,
                        HtmlTab = new HtmlTab(new
                        {
                            ResultImage = new HtmlImage(t.Item.OriginImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(t.Item.TemplateROI)]),
                            TemplateImage = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    })
                ])
            }),
            StageMapCommands.IsDark
        }), HtmlLogUniqueId.LoggingHtml());

        await StageMapCommands.CalibrateAsync(false, HtmlLogUniqueId, cancellationToken).ConfigureAwait(false);

        return true;
    });

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step8Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () =>
    {
        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            Cache.MicroscopeLensInformation,
            DFParam = new HtmlQuote(new
            {
                Cache.ChuckStageMapCacheDFItem.ProductivityInformation,
                Cache.ChuckStageMapCacheDFItem.LaserLightInformation,
                Cache.ChuckStageMapCacheDFItem.CIBInformation,
                OpticsConfiguration = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.CIBConfiguration.ToHtmlAnonymous()),
                TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                Cache.ChuckStageMapCacheDFItem.WaferRadius,
                Cache.ChuckStageMapCacheDFItem.DiePitchWidth,
                Cache.ChuckStageMapCacheDFItem.DiePitchHeight,
                Cache.ChuckStageMapCacheDFItem.ImageWidth,
                Cache.ChuckStageMapCacheDFItem.StageMapRepeatTimes,
                Table = new HtmlTable(
                [
                    .. Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Index().Select(t => new
                    {
                        t.Index,
                        t.Item.FindBFMachinePosition,
                        t.Item.TemplateROI,
                        t.Item.TemplateFilePath,
                        HtmlTab = new HtmlTab(new
                        {
                            ResultImage = new HtmlImage(t.Item.OriginImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(t.Item.TemplateROI)]),
                            TemplateImage = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    })
                ])
            }),
            StageMapCommands.IsDark
        }), HtmlLogUniqueId.LoggingHtml());

        return await StageMapCommands.DFRepeatCalibrateAsync(HtmlLogUniqueId, cancellationToken).ConfigureAwait(false);
    });

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step9Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(() =>
    {
        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            Cache.MicroscopeLensInformation,
            BFParam = new HtmlQuote(new
            {
                TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheBFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                Cache.ChuckStageMapCacheBFItem.WaferRadius,
                Cache.ChuckStageMapCacheBFItem.DiePitchWidth,
                Cache.ChuckStageMapCacheBFItem.DiePitchHeight,
                Table = new HtmlTable(
                [
                    .. Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Index().Select(t => new
                    {
                        t.Index,
                        t.Item.FindBFMachinePosition,
                        t.Item.TemplateROI,
                        t.Item.TemplateFilePath,
                        HtmlTab = new HtmlTab(new
                        {
                            ResultImage = new HtmlImage(t.Item.OriginImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(t.Item.TemplateROI)]),
                            TemplateImage = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    })
                ])
            }),
            DFParam = new HtmlQuote(new
            {
                Cache.ChuckStageMapCacheDFItem.ProductivityInformation,
                Cache.ChuckStageMapCacheDFItem.LaserLightInformation,
                Cache.ChuckStageMapCacheDFItem.CIBInformation,
                OpticsConfiguration = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.CIBConfiguration.ToHtmlAnonymous()),
                TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                Cache.ChuckStageMapCacheDFItem.WaferRadius,
                Cache.ChuckStageMapCacheDFItem.DiePitchWidth,
                Cache.ChuckStageMapCacheDFItem.DiePitchHeight,
                Cache.ChuckStageMapCacheDFItem.ImageWidth,
                Cache.ChuckStageMapCacheDFItem.StageMapRepeatTimes,
                Table = new HtmlTable(
                [
                    .. Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Index().Select(t => new
                    {
                        t.Index,
                        t.Item.FindBFMachinePosition,
                        t.Item.TemplateROI,
                        t.Item.TemplateFilePath,
                        HtmlTab = new HtmlTab(new
                        {
                            ResultImage = new HtmlImage(t.Item.OriginImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(t.Item.TemplateROI)]),
                            TemplateImage = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    })
                ])
            }),
            StageMapCommands.IsDark
        }), HtmlLogUniqueId.LoggingHtml());

        cancellationToken.ThrowIfCancellationRequested();

        CalibratingItem.ExpandStageMap = CalibratingItem.DFStageMap.Expand(
            CalibratingItem.BFStageMap,
            new Circle(MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel), Cache.ChuckStageMapCacheBFItem.WaferRadius),
            new Circle(StageViewModel.DarkFieldToMachinePosition(StageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel))), Cache.ChuckStageMapCacheDFItem.WaferRadius));
        CalibratingItem.ExpandStageMap.Refresh();

        cancellationToken.ThrowIfCancellationRequested();

        CalibratingItem.IsCalibrated = true;
        Guard.IsTrue(Save(CalibratingItem, cancellationToken));

        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlContainer([
            .. CalibratingItem.ExpandStageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
            .. CalibratingItem.ExpandStageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
        ]), HtmlLogUniqueId.LoggingHtml());

        return true;
    });

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(async () =>
        {
            Review.IsVerified = false;
            Review.BFVerifyMaxError = Vector.Zero;
            Review.DFVerifyMaxError = Vector.Zero;

            Logger.LogHtmlInformation("Verify", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.MicroscopeLensInformation,
                BFParam = new HtmlQuote(new
                {
                    TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheBFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                    Cache.ChuckStageMapCacheBFItem.WaferRadius,
                    Cache.ChuckStageMapCacheBFItem.DiePitchWidth,
                    Cache.ChuckStageMapCacheBFItem.DiePitchHeight,
                    Table = new HtmlTable(
                    [
                        .. Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Index().Select(t => new
                        {
                            t.Index,
                            t.Item.FindBFMachinePosition,
                            t.Item.TemplateROI,
                            t.Item.TemplateFilePath,
                            HtmlTab = new HtmlTab(new
                            {
                                ResultImage = new HtmlImage(t.Item.OriginImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(t.Item.TemplateROI)]),
                                TemplateImage = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                            })
                        })
                    ])
                }),
                DFParam = new HtmlQuote(new
                {
                    Cache.ChuckStageMapCacheDFItem.ProductivityInformation,
                    Cache.ChuckStageMapCacheDFItem.LaserLightInformation,
                    Cache.ChuckStageMapCacheDFItem.CIBInformation,
                    OpticsConfiguration = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.OpticsConfiguration.ToHtmlAnonymous()),
                    CIBConfiguration = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.CIBConfiguration.ToHtmlAnonymous()),
                    TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                    Cache.ChuckStageMapCacheDFItem.WaferRadius,
                    Cache.ChuckStageMapCacheDFItem.DiePitchWidth,
                    Cache.ChuckStageMapCacheDFItem.DiePitchHeight,
                    Cache.ChuckStageMapCacheDFItem.ImageWidth,
                    Cache.ChuckStageMapCacheDFItem.StageMapRepeatTimes,
                    Table = new HtmlTable(
                    [
                        .. Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Index().Select(t => new
                        {
                            t.Index,
                            t.Item.FindBFMachinePosition,
                            t.Item.TemplateROI,
                            t.Item.TemplateFilePath,
                            HtmlTab = new HtmlTab(new
                            {
                                ResultImage = new HtmlImage(t.Item.OriginImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(t.Item.TemplateROI)]),
                                TemplateImage = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                            })
                        })
                    ])
                }),
                Cache.Threshold,
                StageMapCommands.IsDark
            }), HtmlLogUniqueId.LoggingHtml());

            Review.ExpandStageMap.Refresh();
            Logger.LogHtmlInformation("Expand StageMap", HtmlHeaderLevelEnum.Header3, new HtmlContainer([
                .. Review.ExpandStageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                .. Review.ExpandStageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
            ]), HtmlLogUniqueId.LoggingHtml());

            #region BF

            StageMapCommands.IsDark = false;
            Logger.LogHtmlInformation("BF", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                StageMapCommands.IsDark
            }), HtmlLogUniqueId.LoggingHtml());

            await StageMapCommands.AlignmentThenApplyTemplatesAsync(
                HtmlLogUniqueId,
                cancellationToken,
                isCalibrate: false).ConfigureAwait(false);

            await StageMapCommands.VerifyAsync(
                Review.ExpandStageMap,
                HtmlLogUniqueId,
                cancellationToken,
                isDownloadStageMap: true,
                isInterpolateErrorsAdaptTo: false).ConfigureAwait(false);

            #endregion

            #region DF

            StageMapCommands.IsDark = true;
            Logger.LogHtmlInformation("DF", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                StageMapCommands.IsDark
            }), HtmlLogUniqueId.LoggingHtml());

            await StageMapCommands.AlignmentThenApplyTemplatesAsync(
                HtmlLogUniqueId,
                cancellationToken,
                isCalibrate: false).ConfigureAwait(false);

            await StageMapCommands.VerifyAsync(
                Review.ExpandStageMap,
                HtmlLogUniqueId,
                cancellationToken,
                isDownloadStageMap: false,
                isInterpolateErrorsAdaptTo: false).ConfigureAwait(false);

            #endregion

            Review.IsVerified = Review.BFVerifyMaxError.Length < Cache.Threshold && Review.DFVerifyMaxError.Length < Cache.Threshold;
            Guard.IsTrue(Save(Review, cancellationToken));

            var htmlQuote = new HtmlQuote(new
            {
                Review.BFVerifyMaxError,
                Review.DFVerifyMaxError,
                Review.IsVerified
            });

            if (Review.IsVerified) Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlQuote, HtmlLogUniqueId.LoggingHtml());
            else Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlQuote, HtmlLogUniqueId.LoggingHtml());

            DialogWindowProvider.ShowDialog($"""
                                             Verify : {(Review.IsVerified ? "OK" : "Failed")}
                                             BF Max Error:{Review.BFVerifyMaxError}
                                             DF Max Error: {Review.DFVerifyMaxError}
                                             """,
                DialogButtonsEnum.OK,
                Review.IsVerified ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return Review.IsVerified;
        }).ConfigureAwait(false);
    }

    private bool Save(ChuckStageMapDTO dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();

        ApplicationCookieService.SetCalibration(dto, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase calibration, CancellationToken cancellationToken)
    {
        var temp = Guard.IsAssignableToTypeAndReturn<ChuckStageMapDTO>(calibration);
        var status = Entry.Status;

        Calibration = temp;

        status.TotalCalibrationCount = 1;
        status.CalibratedCount = Calibration.IsCalibrated ? 1 : 0;
        status.VerifiedCount = Calibration.IsVerified ? 1 : 0;
        status.Details = [];
    }

    #endregion 校准
}