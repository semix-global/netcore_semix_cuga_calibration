using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.StageMap;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using System.IO;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationStageService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationStageServiceMockImpl : ICalibrationStageService
{
    private static readonly (double XDirection, double YDirection) XYDirection = (-1, 1);
    private static readonly Point BrightFieldStagePosition = new(88661.791, -1263.795);
    private static readonly Point DarkFieldStagePosition = new(-80971.341, -21275.932);

    private Point _curPosition = BrightFieldStagePosition;
    private double _curTheta;

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableJoystick(bool enable)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetXSpeedValue(double speedValue)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetYSpeedValue(double speedValue)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetMachineStageTheta()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(_curTheta);
    }

    public SxExecuteRet<bool> MoveRelativeStageTheta(double degrees)
    {
        Thread.Sleep(100);
        _curTheta += degrees;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetAbsoluteStageTheta(double degrees)
    {
        Thread.Sleep(100);
        _curTheta = degrees;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> MoveRelativeStageXy(StageDirectionTypeEnum dir, double step)
    {
        switch (dir)
        {
            case StageDirectionTypeEnum.UpLeft:
                _curPosition += new Vector(XYDirection.XDirection * -step, XYDirection.YDirection * step);
                break;

            case StageDirectionTypeEnum.Up:
                _curPosition += new Vector(XYDirection.XDirection * 0, XYDirection.YDirection * step);
                break;

            case StageDirectionTypeEnum.UpRight:
                _curPosition += new Vector(XYDirection.XDirection * step, XYDirection.YDirection * step);
                break;

            case StageDirectionTypeEnum.Left:
                _curPosition += new Vector(XYDirection.XDirection * -step, XYDirection.YDirection * 0);
                break;

            case StageDirectionTypeEnum.Right:
                _curPosition += new Vector(XYDirection.XDirection * step, XYDirection.YDirection * 0);
                break;

            case StageDirectionTypeEnum.DownLeft:
                _curPosition += new Vector(XYDirection.XDirection * -step, XYDirection.YDirection * -step);
                break;

            case StageDirectionTypeEnum.Down:
                _curPosition += new Vector(XYDirection.XDirection * 0, XYDirection.YDirection * -step);
                break;

            case StageDirectionTypeEnum.DownRight:
                _curPosition += new Vector(XYDirection.XDirection * step, XYDirection.YDirection * -step);
                break;
        }

        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<Point> GetBrightFieldStagePosition() => MachineToBrightFieldPosition(_curPosition);

    public SxExecuteRet<bool> SetBrightFieldAbsoluteStageXy(Point point)
    {
        _curPosition = BrightFieldToMachinePosition(point).Anything;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<Point> GetDarkFieldStagePosition() => MachineToDarkFieldPosition(_curPosition);

    public SxExecuteRet<bool> SetDarkFieldAbsoluteStageXy(Point point)
    {
        _curPosition = DarkFieldToMachinePosition(point).Anything;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<Point> GetMachineStagePosition()
    {
        return SxExecuteRetHelper.CreateSuccess(_curPosition);
    }

    public SxExecuteRet<bool> SetMachineAbsoluteStageXy(Point point)
    {
        _curPosition = point;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetMachineAbsoluteStageXyByFixedSpeed(Point point)
    {
        _curPosition = point;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double XDirection, double YDirection)> GetMachineDirection()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(XYDirection);
    }

    public SxExecuteRet<bool> InitYAxis()
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double BeforeOffset, double AfterOffset)> OriginYOffsetCalibration()
    {
        return SxExecuteRetHelper.CreateSuccess<(double BeforeOffet, double AfterOffset)>((0, 1));
    }

    public SxExecuteRet<Point> BrightFieldToMachinePosition(Point point)
    {
        var temp = new Point(XYDirection.XDirection * point.X, XYDirection.YDirection * point.Y);

        return SxExecuteRetHelper.CreateSuccess(temp + (Vector)BrightFieldStagePosition);
    }

    public SxExecuteRet<Point> DarkFieldToMachinePosition(Point point)
    {
        var temp = new Point(XYDirection.XDirection * point.X, XYDirection.YDirection * point.Y);

        return SxExecuteRetHelper.CreateSuccess(temp + (Vector)DarkFieldStagePosition);
    }

    public SxExecuteRet<Point> MachineToBrightFieldPosition(Point point)
    {
        var temp = point - (Vector)BrightFieldStagePosition;

        return SxExecuteRetHelper.CreateSuccess(new Point(XYDirection.XDirection * temp.X, XYDirection.YDirection * temp.Y));
    }

    public SxExecuteRet<Point> MachineToDarkFieldPosition(Point point)
    {
        var temp = point - (Vector)DarkFieldStagePosition;

        return SxExecuteRetHelper.CreateSuccess(new Point(XYDirection.XDirection * temp.X, XYDirection.YDirection * temp.Y));
    }

    public SxExecuteRet<Point> FindWaferCenterByAutomatic(int offsetThreshold = 100)
    {
        _curPosition = new Point(Random.Shared.Next(1, 100), Random.Shared.Next(1, 100));
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(_curPosition);
    }

    public SxExecuteRet<Point> FindWaferCenterByManually(out List<byte[]> bitmapMemoryBytes, Point offset, List<Point>? waferEdgeOffsets = null)
    {
        _curPosition = new Point(Random.Shared.Next(1, 100), Random.Shared.Next(1, 100));
        var tempBitmap = Convert.FromBase64String(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test.txt")));
        bitmapMemoryBytes = [tempBitmap, tempBitmap, tempBitmap, tempBitmap, tempBitmap, tempBitmap, tempBitmap, tempBitmap];
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(_curPosition);
    }

    public SxExecuteRet<AlignmentSiteDto> MarkAlignSite1(
        AlgorithmTemplateSizeEnum algorithmTemplateSizeEnum,
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum
    )
    {
        _curPosition = new Point(Random.Shared.Next(1, 100), Random.Shared.Next(1, 100));
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(
            new AlignmentSiteDto
            {
                Location = _curPosition,
                Template = new AlignmentTemplateDto
                {
                    Name = "Test",
                    Size = new Size(Convert.ToInt32(algorithmTemplateSizeEnum), Convert.ToInt32(algorithmTemplateSizeEnum)),
                    Thumb = Convert.FromBase64String(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test.txt")))
                }
            }
        );
    }

    public SxExecuteRet<AlignmentSiteDto> MarkAlignSite2(AlignmentSiteDto site, AlgorithmWaferTypeEnum algorithmWaferTypeEnum)
    {
        _curPosition = new Point(Random.Shared.Next(1, 100), Random.Shared.Next(1, 100));
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(
            new AlignmentSiteDto
            {
                Location = _curPosition,
                Template = new AlignmentTemplateDto
                {
                    Name = "Test",
                    Size = site.Template?.Size ?? Size.Empty,
                    Thumb = Convert.FromBase64String(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test.txt")))
                }
            }
        );
    }

    public SxExecuteRet<AlignmentResultDto> Alignment(
        AlignmentSiteDto lowSite1,
        AlignmentSiteDto lowSite2,
        AlignmentSiteDto highSite1,
        AlignmentSiteDto highSite2,
        MicroscopeLensInformation lowMicroscopeLensInformation,
        MicroscopeLensInformation highMicroscopeLensInformation,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum,
        bool isP2
    )
    {
        Thread.Sleep(100);
        var offsetAngle = 0.01 + Random.Shared.NextDouble() * (0.1 - 0.05);

        return SxExecuteRetHelper.CreateSuccess(new AlignmentResultDto { Degrees = offsetAngle });
    }

    public SxExecuteRet<AlignmentResultDto> AlignmentVerify(
        AlignmentSiteDto lowSite1,
        AlignmentSiteDto lowSite2,
        AlignmentSiteDto highSite1,
        AlignmentSiteDto highSite2,
        MicroscopeLensInformation lowMicroscopeLensInformation,
        MicroscopeLensInformation highMicroscopeLensInformation,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum,
        bool isP2)
    {
        Thread.Sleep(100);
        var offsetAngle = 0.01 + Random.Shared.NextDouble() * (0.1 - 0.05);

        return SxExecuteRetHelper.CreateSuccess(new AlignmentResultDto { Degrees = offsetAngle });
    }

    public SxExecuteRet<AlignmentSiteDto> MarkAlignSite1DarkField(
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        ProductivityInformation productivityInformation,
        AlgorithmTemplateSizeEnum algorithmTemplateSizeEnum,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum,
        LaserLightInformation laserLightInformation)
    {
        _curPosition = new Point(Random.Shared.Next(1, 100), Random.Shared.Next(1, 100));
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(
            new AlignmentSiteDto
            {
                Location = _curPosition,
                Template = new AlignmentTemplateDto
                {
                    Name = "Test1",
                    Size = new Size(Convert.ToInt32(algorithmTemplateSizeEnum), Convert.ToInt32(algorithmTemplateSizeEnum)),
                    Thumb = Convert.FromBase64String(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test1.txt")))
                }
            }
        );
    }

    public SxExecuteRet<AlignmentSiteDto> MarkAlignSite2DarkField(
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        ProductivityInformation productivityInformation,
        AlignmentSiteDto site,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum
    )
    {
        _curPosition = new Point(Random.Shared.Next(1, 100), Random.Shared.Next(1, 100));
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(
            new AlignmentSiteDto
            {
                Location = _curPosition,
                Template = new AlignmentTemplateDto
                {
                    Name = "Test1",
                    Size = site.Template?.Size ?? Size.Empty,
                    Thumb = Convert.FromBase64String(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test1.txt")))
                }
            }
        );
    }

    public SxExecuteRet<AlignmentResultDto> AlignmentDarkField(
        AlignmentSiteDto brightFieldLowSite1,
        AlignmentSiteDto brightFieldLowSite2,
        AlignmentSiteDto darkFieldHighSite1,
        AlignmentSiteDto darkFieldHighSite2,
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        ProductivityInformation productivityInformation,
        MicroscopeLensInformation lowMicroscopeLensInformation,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum,
        LaserLightInformation laserLightInformation)
    {
        Thread.Sleep(100);
        var offsetAngle = 0.01 + Random.Shared.NextDouble() * (0.1 - 0.05);

        return SxExecuteRetHelper.CreateSuccess(new AlignmentResultDto { Degrees = offsetAngle });
    }

    public SxExecuteRet<bool> AlignmentBlankWafer()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetGantryOffset(double gantryOffset)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableStageMap(bool enable)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetStageMap(StageMapDto stageMapDto)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetBrightFieldCenterMachinePositionValue(Point position)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetDarkFieldCenterMachinePositionValue(Point position)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<Point> GetBrightFieldCenterMachinePositionValue()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(new Point(89500, -20300));
    }

    public SxExecuteRet<bool> SetXYGlobalScale(double xScale, double yScale)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetTScale(double tScale)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<Point> GetEfemLoadWaferMachineStagePosition()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(new Point(250000, 270000));
    }

    public SxExecuteRet<double> GetEfemLoadWaferMachineStageTheta()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(0.0);
    }
}