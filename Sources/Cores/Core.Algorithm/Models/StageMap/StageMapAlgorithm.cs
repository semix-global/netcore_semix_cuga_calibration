using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Algorithm.Models.TemplateMatch;
using Core.Models;
using Core.Models.Enums.Stage;
using Core.Models.Models.CIB.LineCentricity;
using Core.Models.Models.CIB.XPixelSize;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using HalconDotNet;
using Local.SQL.Cache.Providers.Services.Interfaces;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Editors.Getters.Options;
using Net.Utilities.Graphics.Primitives.Enums.Editors;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WaferMap.WPF.Primitives;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using Python.Runtime;
using System.Collections;
using System.IO;
using System.Reflection;
using Constants = Net.Utilities.Models.Constants;

namespace Core.Algorithm.Models.StageMap;

public static class StageMapAlgorithm
{
    private static readonly string ClosedLoopCalibrationPythonScript = AssemblyHelper.GetEmbeddedResource("Assets.Python.closed_loop_calibration.py");

    private static void ProcessFirstMeasurement(StageMap stageMap)
    {
        using var _ = Py.GIL();
        using var module = PyModule.FromString("closed_loop_calibration", ClosedLoopCalibrationPythonScript);
        using var process = module.GetAttr("process_first_measurement");

        using var pyResidual = stageMap.ToPythonErrorMatrix();
        using var pyDesiredPositions = stageMap.ToPythonIdealMatrix();
        using var pyMask = stageMap.ToPythonIsMatchMatrix();
        using var pyFillValue = 0d.ToPython();
        using var result = process.Invoke(pyResidual, pyDesiredPositions, pyMask, pyFillValue);

        stageMap.ApplyPythonErrorMatrix(result);
    }

    private bool ProcessStage2Residuals(IReadOnlyList<StageMap> historyStageMaps, StageMap scanStageMap)
    {
        Guard.IsNotEmpty(historyStageMaps);

        using var _ = Py.GIL();
        using var module = PyModule.FromString("closed_loop_calibration", ClosedLoopCalibrationPythonScript);
        using var process = module.GetAttr("process_stage2_residuals");

        using var pyResiduals = new PyList();
        using var pyMasks = new PyList();
        foreach (var stageMap in historyStageMaps)
        {
            using var pyResidual = stageMap.ToPythonErrorMatrix();
            using var pyMask = stageMap.ToPythonIsMatchMatrix();

            pyResiduals.Append(pyResidual);
            pyMasks.Append(pyMask);
        }

        using var pyDesiredPositions = scanStageMap.ToPythonIdealMatrix();
        using var pyTargetValidCount = Cache.StageMapRepeatTimes.ToPython();
        using var result = process.Invoke(pyResiduals, pyDesiredPositions, pyTargetValidCount, pyMasks);

        using var pyNeedMoreMeasurement = Guard.IsNotNullAndReturn(result[0]);
        using var pyResidualTable = Guard.IsNotNullAndReturn(result[1]);
        using var pyStage2ValidMask = Guard.IsNotNullAndReturn(result[2]);

        var needMoreMeasurement = pyNeedMoreMeasurement.As<bool>();

        scanStageMap.ApplyPythonErrorMatrix(pyResidualTable);
        scanStageMap.ApplyPythonIsMatchMatrix(pyStage2ValidMask);

        return needMoreMeasurement == false;
    }

    private void CombineCorrectionTables(StageMap residualStageMap)
    {
        using var _ = Py.GIL();
        using var module = PyModule.FromString("closed_loop_calibration", ClosedLoopCalibrationPythonScript);
        using var combine = module.GetAttr("combine_correction_tables");

        // 阶段 1 mask 表示初始修正值的来源, 阶段 2 mask 表示残差是否可参与合并.
        using var pyInitialCorrection = Cache.StageMap.ToPythonErrorMatrix();
        using var pyResidualTable = residualStageMap.ToPythonErrorMatrix();
        using var pyStage1Mask = Cache.StageMap.ToPythonIsMatchMatrix();
        using var pyStage2ValidMask = residualStageMap.ToPythonIsMatchMatrix();
        using var pyXGroupSize = Cache.StageMapTemplates.Length.ToPython();
        using var result = combine.Invoke(pyInitialCorrection, pyResidualTable, pyStage1Mask, pyStage2ValidMask, pyXGroupSize);

        using var pyFinalCorrection = Guard.IsNotNullAndReturn(result[0]);
        using var pyInterpolationMask = Guard.IsNotNullAndReturn(result[1]);

        Cache.StageMap.ApplyPythonErrorMatrix(pyFinalCorrection);
        Cache.StageMap.ApplyPythonIsMatchMatrix(pyInterpolationMask);
    }
}