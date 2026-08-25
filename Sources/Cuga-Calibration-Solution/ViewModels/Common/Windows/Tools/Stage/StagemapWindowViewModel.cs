using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.Cookies;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WaferMap.WPF.Primitives.Builders;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections;
using System.IO;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

[IOCAppService(ServiceType = typeof(StagemapWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class StagemapWindowViewModel(
    CIBViewModel cibViewModel,
    MicroscopeViewModel microscopeViewModel,
    ReviewViewModel reviewViewModel,
    StageViewModel stageViewModel,
    CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel,
    AlignmentUserControlViewModel alignmentUserControlViewModel,
    IApplicationCookieService applicationCookieService,
    ICacheProvider cacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    IWindowManagerService windowManagerService,
    IOptions<ApplicationSetting> options,
    ApplicationCookie applicationCookie,
    ILogger<StagemapWindowViewModel> logger) : ViewModelBase
{
    public ApplicationCookie ApplicationCookie => applicationCookie;

    public string Name => "Stagemap Diagnostic Tool";

    public string TemplateFileDirectory => Path.Combine(
        options.Value.AppHomeDirectory,
        "Template",
        nameof(StagemapWindowViewModel),
        DateTime.Now.ToString("yyyyMMdd"));

    public string ResultImageDirectory => Path.Combine(
        options.Value.AppHomeDirectory,
        "Images",
        nameof(StagemapWindowViewModel),
        DateTime.Now.ToString("yyyyMMdd"));

    public IReadOnlyList<string> Steps { get; } =
    [
        "Step 1 Alignment",
        "Step 2 Generate Wafer Map",
        "Step 3 Scan Stagemap",
        "Step 4 Repeated Scan",
        "Step 5 Verification Scan"
    ];

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = alignmentUserControlViewModel;

    [DefaultCache]
    [ObservableProperty]
    public partial StagemapCache Cache { get; set; } = new();

    [RelayCommand]
    private async Task LoadedAsync()
    {
        Cache = await Task.Run(() => cacheProvider.GetOrDefault<StagemapCache>()).ConfigureAwait(true);

        AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
        AlignmentUserControlViewModel.ProductivityInformation = Cache.ProductivityInformation;
    }

    [RelayCommand]
    private void AddTemplatePoint()
    {
        Cache.TemplatePoints = [.. Cache.TemplatePoints, new StagemapTemplatePoint()];
    }

    [RelayCommand]
    private void RemoveTemplatePoints(IEnumerable? selectedItems)
    {
        if (selectedItems is null) return;

        var templatePoints = Cache.TemplatePoints.ToList();
        foreach (StagemapTemplatePoint selectedItem in selectedItems) templatePoints.Remove(selectedItem);

        Cache.TemplatePoints = templatePoints;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task MarkTemplatePointAsync(StagemapTemplatePoint? templatePoint, CancellationToken cancellationToken)
    {
        if (templatePoint is null) return;

        try
        {
            ValidateImageParameters();
            Guard.IsEqualTo(Cache.MicroscopeLensInformation, microscopeViewModel.GetCurrentMicroscopeLensInformation());

            var templatePointIndex = Cache.TemplatePoints.ToList().IndexOf(templatePoint);
            Guard.IsGreaterThanOrEqualTo(templatePointIndex, 0);

            var findBrightFieldMachinePosition = stageViewModel.GetMachineStagePosition();
            var yPixelSize = GetYPixelSize(cancellationToken);

            if (templatePointIndex > 0)
            {
                ValidateTemplatePointPosition(
                    findBrightFieldMachinePosition,
                    Cache.TemplatePoints[0].FindBrightFieldMachinePosition,
                    yPixelSize,
                    Cache.ImageHeight);
            }

            var templateDirectory = Path.Combine(TemplateFileDirectory, Cache.MicroscopeLensInformation.LensName);
            Directory.CreateDirectory(templateDirectory);

            var brightTemplateFilePath = Path.Combine(templateDirectory, $"{Guid.NewGuid():N}_Bright");
            Guard.IsTrue(reviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, brightTemplateFilePath, Cache.AlgorithmTemplateSizeEnum));

            var brightTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(brightTemplateFilePath);
            using var darkFieldImage = await cibViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Bright,
                stageViewModel.MachineToBrightFieldPosition(findBrightFieldMachinePosition),
                Cache.ImageWidth,
                Cache.CIBInformation,
                (false, CalChipSiteModelEnum.ChuckModel),
                (false, Cache.OpticsConfiguration),
                (false, Cache.CIBConfiguration),
                (false, Cache.LaserLightInformation),
                false,
                cancellationToken);

            if (templatePointIndex > 0)
            {
                ValidateTemplatePointPosition(
                    findBrightFieldMachinePosition,
                    Cache.TemplatePoints[0].FindBrightFieldMachinePosition,
                    yPixelSize,
                    (int)darkFieldImage.Size.Height);
            }
            else
            {
                foreach (var existingTemplatePoint in Cache.TemplatePoints.Skip(1).Where(t => string.IsNullOrWhiteSpace(t.DarkTemplateFilePath) == false))
                {
                    ValidateTemplatePointPosition(
                        existingTemplatePoint.FindBrightFieldMachinePosition,
                        findBrightFieldMachinePosition,
                        yPixelSize,
                        (int)darkFieldImage.Size.Height);
                }
            }

            var darkOriginImageFilePath = Path.Combine(templateDirectory, $"{Guid.NewGuid():N}_DarkOrigin.jpg");
            var darkTemplateFilePath = $"{darkOriginImageFilePath}_Template";
            darkFieldImage.Image.SaveImage(darkOriginImageFilePath);

            createDarkImageTemplateWindowViewModel.ImageFilePath = darkOriginImageFilePath;
            createDarkImageTemplateWindowViewModel.TemplateFilePath = darkTemplateFilePath;
            createDarkImageTemplateWindowViewModel.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
            createDarkImageTemplateWindowViewModel.AlgorithmTemplateSizeEnum = Cache.AlgorithmTemplateSizeEnum;

            Guard.IsTrue(windowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel) == true, nameof(createDarkImageTemplateWindowViewModel));

            templatePoint.FindBrightFieldMachinePosition = findBrightFieldMachinePosition;
            templatePoint.BrightTemplateFilePath = brightTemplateFilePath;
            templatePoint.BrightTemplateImageFilePath = brightTemplateImageFilePath;
            templatePoint.DarkTemplateFilePath = darkTemplateFilePath;
            templatePoint.DarkTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(darkTemplateFilePath);
            Cache.ImageHeight = (int)darkFieldImage.Size.Height;

            dialogWindowProvider.ShowDialog(
                $"Template point {templatePointIndex + 1} captured successfully.",
                DialogButtonsEnum.OK,
                DialogIconEnum.Information);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("{Name}: template point capture canceled", Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Name}: capture template point failed", Name);
            dialogWindowProvider.ShowDialog($"{Name}: Capture Template Point Failed\r\n{ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step0Async(CancellationToken cancellationToken)
    {
        await ExecuteOperationAsync("Alignment", async () =>
        {
            AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
            AlignmentUserControlViewModel.ProductivityInformation = Cache.ProductivityInformation;

            dialogWindowProvider.TryShowDialog(
                "Yes: use dark field alignment? No: use bright field alignment?",
                out var dialogResult,
                DialogButtonsEnum.YesNo,
                DialogIconEnum.Question);

            AlignmentUserControlViewModel.IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;

            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            Cache.AlignmentResult = AlignmentUserControlViewModel.AlignmentResult;

            logger.LogInformation(
                "{Name}: alignment completed in Chuck mode. IsDarkFieldAlignment: {IsDarkFieldAlignment}",
                Name,
                AlignmentUserControlViewModel.IsDarkFieldAlignment);
        }, () => "Alignment completed.");
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await ExecuteOperationAsync("Generate Wafer Map", () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidateBuildParameters();

            Cache.Stagemap = BuildStagemap(cancellationToken);
            Cache.RepeatedStagemaps = [];
            Cache.VerifyStagemap = new Stagemap();

            return Task.CompletedTask;
        }, () => $"Generate Wafer Map completed. Matrix: {Cache.Stagemap.IdealPoint.GetLength(0)} x {Cache.Stagemap.IdealPoint.GetLength(1)}.");
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step2Async(CancellationToken cancellationToken)
    {
        var matchCount = 0;
        var totalCount = 0;
        await ExecuteOperationAsync("Scan Stagemap", async () =>
        {
            (matchCount, totalCount) = await ScanStagemapAsync(Cache.Stagemap, "Scan", cancellationToken);
        }, () => $"Scan Stagemap completed. Match: {matchCount}/{totalCount}.");
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step3Async(CancellationToken cancellationToken)
    {
        var totalMatchCount = 0;
        var totalPointCount = 0;
        await ExecuteOperationAsync("Repeated Scan", async () =>
        {
            ValidateStagemap(Cache.Stagemap);
            Guard.IsGreaterThan(Cache.RepeatCount, 0);

            var repeatedStagemaps = new List<Stagemap>(Cache.RepeatCount);
            for (var repeatIndex = 0; repeatIndex < Cache.RepeatCount; repeatIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var repeatedStagemap = Cache.Stagemap.Clone();
                var (matchCount, pointCount) = await ScanStagemapAsync(repeatedStagemap, $"Repeat_{repeatIndex + 1}", cancellationToken);
                totalMatchCount += matchCount;
                totalPointCount += pointCount;
                repeatedStagemaps.Add(repeatedStagemap);
                Cache.RepeatedStagemaps = [.. repeatedStagemaps];
            }
        }, () => $"Repeated Scan completed. Repeat: {Cache.RepeatedStagemaps.Count}, Match: {totalMatchCount}/{totalPointCount}.");
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step4Async(CancellationToken cancellationToken)
    {
        var matchCount = 0;
        var totalCount = 0;
        await ExecuteOperationAsync("Verification Scan", async () =>
        {
            ValidateStagemap(Cache.Stagemap);
            Cache.VerifyStagemap = Cache.Stagemap.Clone();
            (matchCount, totalCount) = await ScanStagemapAsync(Cache.VerifyStagemap, "Verify", cancellationToken);
        }, () => $"Verification Scan completed. Match: {matchCount}/{totalCount}.");
    }

    private void ValidateImageParameters()
    {
        Guard.IsGreaterThan(Cache.ImageWidth, 0);
        Guard.IsGreaterThan(Cache.DiePitchSize.Width, 0d);
        Guard.IsGreaterThan(Cache.DiePitchSize.Height, 0d);
    }

    private void ValidateBuildParameters()
    {
        ValidateImageParameters();
        Guard.IsGreaterThan(Cache.WaferDiameter, 0d);
        Guard.IsNotEmpty(Cache.TemplatePoints);

        foreach (var templatePoint in Cache.TemplatePoints)
        {
            Guard.IsNotNullOrWhiteSpace(templatePoint.BrightTemplateFilePath);
            Guard.IsNotNullOrWhiteSpace(templatePoint.DarkTemplateFilePath);
        }
    }

    private double GetYPixelSize(CancellationToken cancellationToken)
    {
        var yPixelSize = applicationCookieService.GetCalibrations<CIBYPixelSizeDTO>(cancellationToken)
            .SingleOrDefault(t => t.ProductivityInformation.OpticsIlluminationModeEnum == Cache.ProductivityInformation.OpticsIlluminationModeEnum
                                  && t.ProductivityInformation.OpticsMagType == Cache.ProductivityInformation.OpticsMagType
                                  && t.PmtId == Cache.CIBInformation.PMTId);

        Guard.IsNotNull(yPixelSize);
        Guard.IsTrue(yPixelSize.IsOk);

        return yPixelSize.YPixelSize;
    }

    private void ValidateTemplatePointPosition(Point position, Point firstPosition, double yPixelSize, int imageHeight)
    {
        var offset = position - firstPosition;
        Guard.IsLessThanOrEqualTo(Math.Abs(offset.X), Cache.DiePitchSize.Width);
        Guard.IsLessThanOrEqualTo(Math.Abs(offset.Y), Cache.DiePitchSize.Height);
        Guard.IsGreaterThan(imageHeight, 0);

        var maximumYOffset = imageHeight * yPixelSize * 2d / 3d;
        Guard.IsLessThanOrEqualTo(Math.Abs(offset.Y), maximumYOffset);
    }

    private Stagemap BuildStagemap(CancellationToken cancellationToken)
    {
        var waferMapDocuments = Cache.TemplatePoints
            .Select((templatePoint, templatePointIndex) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var originalDiePoint = stageViewModel.MachineToBrightFieldPosition(templatePoint.FindBrightFieldMachinePosition);
                var waferMapDieBuilder = new WaferMapDieBuilder
                {
                    DiePitchSize = Cache.DiePitchSize,
                    OriginalDiePoint = originalDiePoint
                };
                var dies = waferMapDieBuilder.BuildDie(new Circle(originalDiePoint, Cache.WaferDiameter / 2d));
                Guard.IsNotEmpty(dies);
                Guard.IsTrue(dies.Any(t => t.Index.X == 0 && t.Index.Y == 0));

                return (TemplatePointIndex: templatePointIndex, OriginalDiePoint: originalDiePoint, Dies: dies);
            })
            .ToArray();

        var baseDocument = waferMapDocuments.Single(t => t.TemplatePointIndex == 0);
        var minimumDieIndexX = baseDocument.Dies.Min(t => t.Index.X);
        var maximumDieIndexX = baseDocument.Dies.Max(t => t.Index.X);
        var minimumDieIndexY = baseDocument.Dies.Min(t => t.Index.Y);
        var maximumDieIndexY = baseDocument.Dies.Max(t => t.Index.Y);
        var templatePointIndexes = waferMapDocuments
            .OrderBy(t => t.OriginalDiePoint.X)
            .ThenByDescending(t => t.OriginalDiePoint.Y)
            .Select(t => t.TemplatePointIndex)
            .ToArray();

        var rowCount = maximumDieIndexY - minimumDieIndexY + 1;
        var columnCount = (maximumDieIndexX - minimumDieIndexX + 1) * templatePointIndexes.Length;
        var idealPoint = new Point[rowCount, columnCount];
        var realPoint = new Point[rowCount, columnCount];
        var errorPoint = new Point[rowCount, columnCount];
        var validPoint = new bool[rowCount, columnCount];
        var documentDieDictionaries = waferMapDocuments.ToDictionary(
            t => t.TemplatePointIndex,
            t => t.Dies.ToDictionary(die => die.Index));

        foreach (var baseDie in baseDocument.Dies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = maximumDieIndexY - baseDie.Index.Y;
            var firstColumn = (baseDie.Index.X - minimumDieIndexX) * templatePointIndexes.Length;

            for (var templateOrderIndex = 0; templateOrderIndex < templatePointIndexes.Length; templateOrderIndex++)
            {
                var templatePointIndex = templatePointIndexes[templateOrderIndex];
                Guard.IsTrue(documentDieDictionaries[templatePointIndex].TryGetValue(baseDie.Index, out var die));

                var column = firstColumn + templateOrderIndex;
                idealPoint[row, column] = new Point(die.Rect.Point.X, die.Rect.Point.Y);
                validPoint[row, column] = true;
            }
        }

        return new Stagemap
        {
            IdealPoint = idealPoint,
            RealPoint = realPoint,
            ErrorPoint = errorPoint,
            ValidPoint = validPoint,
            TemplatePointIndexes = templatePointIndexes
        };
    }

    private async Task<(int MatchCount, int TotalCount)> ScanStagemapAsync(Stagemap stagemap, string scanName, CancellationToken cancellationToken)
    {
        ValidateStagemap(stagemap);
        ValidateImageParameters();

        var rowCount = stagemap.IdealPoint.GetLength(0);
        var columnCount = stagemap.IdealPoint.GetLength(1);
        stagemap.RealPoint = new Point[rowCount, columnCount];
        stagemap.ErrorPoint = new Point[rowCount, columnCount];

        var firstTemplateOrderIndex = Array.IndexOf(stagemap.TemplatePointIndexes, 0);
        Guard.IsGreaterThanOrEqualTo(firstTemplateOrderIndex, 0);

        var operationDirectory = Path.Combine(ResultImageDirectory, scanName, $"{DateTime.Now:HHmmssfff}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(operationDirectory);

        var matchCount = 0;
        var totalCount = 0;
        for (var row = 0; row < rowCount; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var validColumns = Enumerable.Range(0, columnCount).Where(column => stagemap.ValidPoint[row, column]).ToArray();
            if (validColumns.Length == 0) continue;

            var centerPositions = validColumns.Select(column =>
            {
                var templateOrderIndex = column % stagemap.TemplatePointIndexes.Length;
                var firstTemplateColumn = column - templateOrderIndex + firstTemplateOrderIndex;
                Guard.IsTrue(stagemap.ValidPoint[row, firstTemplateColumn]);

                var idealPoint = stagemap.IdealPoint[row, column];
                return new Point(idealPoint.X, stagemap.IdealPoint[row, firstTemplateColumn].Y);
            }).ToArray();

            var darkFieldImages = await cibViewModel.GetPMTImagesAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Bright,
                centerPositions,
                Cache.ImageWidth,
                Cache.CIBInformation,
                (false, CalChipSiteModelEnum.ChuckModel),
                (false, Cache.OpticsConfiguration),
                (false, Cache.CIBConfiguration),
                (false, Cache.LaserLightInformation),
                false,
                cancellationToken);

            try
            {
                Guard.IsEqualTo(darkFieldImages.Count, validColumns.Length);

                for (var imageIndex = 0; imageIndex < validColumns.Length; imageIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var column = validColumns[imageIndex];
                    var templateOrderIndex = column % stagemap.TemplatePointIndexes.Length;
                    var templatePointIndex = stagemap.TemplatePointIndexes[templateOrderIndex];
                    Guard.IsGreaterThanOrEqualTo(templatePointIndex, 0);
                    Guard.IsLessThan(templatePointIndex, Cache.TemplatePoints.Count);

                    var templatePoint = Cache.TemplatePoints[templatePointIndex];
                    var cellDirectory = Path.Combine(operationDirectory, $"Row_{row + 1}_Column_{column + 1}_Template_{templatePointIndex + 1}");
                    Directory.CreateDirectory(cellDirectory);
                    totalCount++;

                    if (cibViewModel.TryGetMatchPosition(
                            Cache.ProductivityInformation,
                            StageCoordinateSystemEnum.Bright,
                            centerPositions[imageIndex],
                            Cache.CIBInformation,
                            Cache.AlgorithmTemplateTypeEnum,
                            darkFieldImages[imageIndex],
                            templatePoint.DarkTemplateFilePath,
                            cellDirectory,
                            Guid.NewGuid(),
                            out var realPoint,
                            out _,
                            out _,
                            out _) == false)
                    {
                        logger.LogWarning("{Name}: template match failed at row {Row}, column {Column}, template point {TemplatePoint}", Name, row, column, templatePointIndex);
                        continue;
                    }

                    var idealPoint = stagemap.IdealPoint[row, column];
                    stagemap.RealPoint[row, column] = new Point(realPoint.X, realPoint.Y);
                    var error = realPoint - idealPoint;
                    stagemap.ErrorPoint[row, column] = new Point(error.X, error.Y);
                    matchCount++;
                }
            }
            finally
            {
                foreach (var darkFieldImage in darkFieldImages) darkFieldImage.Dispose();
            }
        }

        return (matchCount, totalCount);
    }

    private static void ValidateStagemap(Stagemap stagemap)
    {
        Guard.IsGreaterThan(stagemap.IdealPoint.Length, 0);
        Guard.IsGreaterThan(stagemap.ValidPoint.Length, 0);
        Guard.IsNotEmpty(stagemap.TemplatePointIndexes);
        Guard.IsEqualTo(stagemap.IdealPoint.GetLength(0), stagemap.ValidPoint.GetLength(0));
        Guard.IsEqualTo(stagemap.IdealPoint.GetLength(1), stagemap.ValidPoint.GetLength(1));
        Guard.IsEqualTo(stagemap.IdealPoint.GetLength(0), stagemap.RealPoint.GetLength(0));
        Guard.IsEqualTo(stagemap.IdealPoint.GetLength(1), stagemap.RealPoint.GetLength(1));
        Guard.IsEqualTo(stagemap.IdealPoint.GetLength(0), stagemap.ErrorPoint.GetLength(0));
        Guard.IsEqualTo(stagemap.IdealPoint.GetLength(1), stagemap.ErrorPoint.GetLength(1));
    }

    private async Task ExecuteOperationAsync(string operationName, Func<Task> operation, Func<string> successMessage)
    {
        try
        {
            await operation();
            dialogWindowProvider.ShowDialog(successMessage(), DialogButtonsEnum.OK, DialogIconEnum.Information);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("{Name}: {OperationName} canceled", Name, operationName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Name}: {OperationName} failed", Name, operationName);
            dialogWindowProvider.ShowDialog($"{Name}: {operationName} Failed\r\n{ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private void Close()
    {
        try
        {
            MarkTemplatePointCancelCommand.Execute(null);
            Step4CancelCommand.Execute(null);
            Step3CancelCommand.Execute(null);
            Step2CancelCommand.Execute(null);
            Step1CancelCommand.Execute(null);
            Step0CancelCommand.Execute(null);

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            cacheProvider.Set(Cache, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Name}: failed to save cache", Name);
        }

        CloseView(null);
    }
}