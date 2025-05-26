using CommunityToolkit.Mvvm.ComponentModel;
using Cuga.Data.DataStruct.Basic;
using Cuga.Engine.Interface;
using CugaCalibration.ViewModels.Common;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithm.Halcon.Helper;
using Net.Utilities.Helper.File;
using Net.Utilities.Models;
using Semix.CoreLib;
using System.IO;

// ReSharper disable LocalizableElement

namespace CugaScript.Core;

[Obsolete]
public sealed partial class ScriptHelper(StageViewModel stageViewModel, ReviewViewModel reviewViewModel, ILogger<ScriptHelper> logger) : ObservableObject
{
    [ObservableProperty]
    private StageViewModel _stageViewModel = stageViewModel;

    [ObservableProperty]
    private ReviewViewModel _reviewViewModel = reviewViewModel;

    [ObservableProperty]
    private ILogger<ScriptHelper> _logger = logger;

    public CancellationToken CancellationToken { get; set; }

    public void Script1()
    {
        var ep = new SxWcfEndPoint("127.0.0.1", 80, CgInernalAddr.CalAddr);
        var cgCalibrationService = SxWcfManager.FindService<ICgCalibrationService>(ep);

        DirectoryHelper.CreateDirectoryIfNotExists("D:\\TestScript1");

        // 循环行列取图片
        foreach (var x in Enumerable.Range(0, 2))
        foreach (var y in Enumerable.Range(0, 2))
        {
            var sxRet = cgCalibrationService.ToBFWaferPosition(x * 2000, y * 2000);
            if (sxRet.IsSuccess == false)
            {
                Logger.LogError(sxRet.ErrorMsg);
                return;
            }

            Thread.Sleep(2000);

            var sxExecuteRet = cgCalibrationService.GetReviewImage();
            if (sxExecuteRet.IsSuccess == false)
            {
                Logger.LogError(sxExecuteRet.ErrorMsg);
                return;
            }

            var path = $"D:\\TestScript1\\{x}_{y}.jpg";
            FileHelper.DeleteFileIfExists(path);

            File.WriteAllBytes(path, sxExecuteRet.Anything);
            Logger.LogInformation($"Get Image OK: {path}");
            CancellationToken.ThrowIfCancellationRequested();
        }
    }

    public void Script2()
    {
        DirectoryHelper.CreateDirectoryIfNotExists("D:\\TestScript2");

        // 循环行列取图片
        foreach (var x in Enumerable.Range(0, 2))
        foreach (var y in Enumerable.Range(0, 2))
        {
            StageViewModel.SetBrightFieldAbsoluteStageXy(new Point(x * 2000, y * 2000));
            Thread.Sleep(1000);

            using var result = ReviewViewModel.GetBrightFieldImage();

            var path = $"D:\\TestScript2\\{x}_{y}.jpg";
            FileHelper.DeleteFileIfExists(path);

            HalconHelper.Save(result, path);
            Logger.LogInformation($"Get Image OK: {path}");
            CancellationToken.ThrowIfCancellationRequested();
        }
    }
}