using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Helper;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Recipe.Models.Wafer;
using Core.Recipe.Models.Wafer.ReticleMask;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common;
using Local.SQL.Cache.Providers.Extensions;
using Local.SQL.Cache.Providers.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WaferMap.WPF.Drawables;

namespace CugaCalibration.Core.Services.Implements;

[IOCAppService(ServiceType = typeof(ICalibrationRecipeService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class CalibrationRecipeServiceImpl(
    [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
    ICacheProvider cacheProvider,
    ILogger<CalibrationRecipeServiceImpl> logger,
    StageViewModel stageViewModel) : ICalibrationRecipeService
{
    public void GetCorrectWaferMapByOffset(WaferDTO waferDto, bool isAutoAlignment)
    {
        var (xDirection, yDirection) = stageViewModel.GetMachineDirection();
        var offsetPosition = Point.Origin;
        if (isAutoAlignment)
        {
            // 对准缓存
            var alignmentCacheBrightField = cacheProvider.GetOrDefault<AlignmentCacheBrightField>();
            // 重新对准
            var alignmentResult = stageViewModel.Alignment(alignmentCacheBrightField.LowSite1, alignmentCacheBrightField.LowSite2,
                alignmentCacheBrightField.HighSite1, alignmentCacheBrightField.HighSite2,
                alignmentCacheBrightField.LowMag, alignmentCacheBrightField.HighMag,
                alignmentCacheBrightField.AlgorithmWaferTypeEnum);

            // 配方对准结果缓存
            var recipeAlignmentResult = waferDto.AlignmentResultDto;

            // 当前对准与配方对准结果的偏移量（waferMap偏移值）
            var offsetX = (alignmentResult.MarkPoint1.X - recipeAlignmentResult!.MarkPoint1.X +
                           (alignmentResult.MarkPoint2.X - recipeAlignmentResult.MarkPoint2.X)) / 2;
            var offsetY = (alignmentResult.MarkPoint1.Y - recipeAlignmentResult.MarkPoint1.Y +
                           (alignmentResult.MarkPoint2.Y - recipeAlignmentResult.MarkPoint2.Y)) / 2;
            offsetPosition = new Point(xDirection * offsetX, yDirection * offsetY);
        }

        var newWaferCenterPosition = waferDto.WaferMapDataDTO.WaferCircleCenter + (Vector)offsetPosition;
        waferDto.WaferMapDataToWaferMapCanvasDocument();

        // 只修改WaferMapCanvasDocument，不修改WaferMapDataDTO
        // WaferMapDataDTO是原始配方数据，仅在配方编辑build wafer时使用，无需修改。
        // 应用配方只关注WaferMapCanvasDocument
        waferDto.WaferMapCanvasDocument.WaferBuilder.Circle = new Circle(
            newWaferCenterPosition.X,
            newWaferCenterPosition.Y,
            waferDto.WaferMapCanvasDocument.WaferBuilder.Circle.Radius);

        waferDto.WaferMapCanvasDocument.DieBuilder.OriginalDiePoint += (Vector)offsetPosition;
        waferDto.WaferMapCanvasDocument.ReticleBuilder.OriginalDiePoint += (Vector)offsetPosition;
    }

    public WaferMapDie GetCurrentWaferMapDie(WaferDTO waferDto, Point machinePosition)
    {
        var waferPosition = stageViewModel.MachineToBrightFieldPosition(machinePosition);
        return waferDto.WaferMapCanvasDocument.DieModel
            .Single(t => t.Rect.Contains(waferPosition));
    }

    public WaferMapReticle GetCurrentWaferMapReticle(WaferDTO waferDto, Point machinePosition)
    {
        var waferPosition = stageViewModel.MachineToBrightFieldPosition(machinePosition);
        return waferDto.WaferMapCanvasDocument.ReticleModel
            .Single(t => t.Rect.Contains(waferPosition));
    }

    public bool GetWaferMapDieMachinePosition<T>(WaferDTO waferDto, WaferMapDie<T> waferMapDie, out Point position) where T : Net.Utilities.Graphics.Primitives.Medias.Layer, new()
    {
        try
        {
            // 使用P8结果Build Wafer，Wafer坐标等价于明场坐标，可以和机械坐标互相转换
            var waferPosition = waferMapDie.Rect.Point;
            position = stageViewModel.BrightFieldToMachinePosition(waferPosition);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get die mask bright field position failed");
            position = Point.Origin;
            return false;
        }
    }

    public bool GetMaskMachinePosition(WaferDTO waferDto, WaferMapDie waferMapDie, ReticleMarkDTOItem mask, bool isReticle, out Point position)
    {
        try
        {
            // 使用P8结果Build Wafer，Wafer坐标等价于明场坐标，可以和机械坐标互相转换
            var maskWaferPosition = waferMapDie.Rect.Point + (Vector)mask.MaskWaferCellPosition;
            position = stageViewModel.BrightFieldToMachinePosition(maskWaferPosition);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get die mask bright field position failed");
            position = Point.Origin;
            return false;
        }
    }

    public bool GetMicroscopeReticleMaskInfo(ReticleMarkDTO reticleMarkDto, WaferMaskTypeEnum waferMaskType, MicroscopeLensInformation? microscopeLensInformation, ProductivityInformation? productivityInformation, out ReticleMarkDTOItem maskInfo)
    {
        try
        {
            maskInfo = reticleMarkDto.MicroscopeReticleMarks
                .First(t => t.ReticleMaskTypeEnum == waferMaskType
                            && (microscopeLensInformation is null || (t.RecipeBrightFieldTemplateDTO.MicroscopeLensInformation == microscopeLensInformation && t.RecipeBrightFieldTemplateDTO.TemplateFilePath != string.Empty))
                            && (productivityInformation is null || (t.RecipeDarkFieldTemplateDTO.ProductivityInformation == productivityInformation && t.RecipeDarkFieldTemplateDTO.TemplateFilePath != string.Empty))
                );
            return true;
        }
        catch (Exception ex)
        {
            maskInfo = new ReticleMarkDTOItem();
            logger.LogError(ex, "Get microscope reticle mask info failed");
            return false;
        }
    }

    public bool GetChuckReticleMaskInfo(ReticleMarkDTO reticleMarkDto, WaferMaskTypeEnum waferMaskType, MicroscopeLensInformation? microscopeLensInformation, ProductivityInformation? opticsMagType, out ReticleMarkDTOItem maskInfo)
    {
        throw new NotImplementedException();
    }

    public bool GetLaserReticleMaskMachineInfo(ReticleMarkDTO reticleMarkDto, WaferMaskTypeEnum waferMaskType, MicroscopeLensInformation? microscopeLensInformation, ProductivityInformation? opticsMagType, out ReticleMarkDTOItem maskInfo)
    {
        throw new NotImplementedException();
    }
}