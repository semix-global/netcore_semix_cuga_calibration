using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.LineCentricity;

public sealed partial class LaserLineCentricityItemDto : CalibrationDtoBase, ICloneable<LaserLineCentricityItemDto>, IAdaptTo<CalibrationLaserLineCentricityItem>
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private int _pmtId;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private Point _findBrightMachinePosition;

    [ObservableProperty]
    private Point _findDarkMachinePosition;

    [ObservableProperty]
    private Point _darkMachineCenterPosition;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _templateFilePath = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath = string.Empty;

    #region Mapper

    public LaserLineCentricityItemDto Clone()
    {
        return new LaserLineCentricityItemDto
        {
            MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
            ProductivityInformation = ProductivityInformation.Clone(),
            PmtId = PmtId,
            FindPosition = FindPosition,
            FindBrightMachinePosition = FindBrightMachinePosition,
            FindDarkMachinePosition = FindDarkMachinePosition,
            DarkMachineCenterPosition = DarkMachineCenterPosition,
            FilePath = FilePath,
            TemplateFilePath = TemplateFilePath,
            TemplateImageFilePath = TemplateImageFilePath,
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredSelfCheck = IsRequiredSelfCheck,
            Id = Id,
            Expiration = Expiration
        };
    }

    public CalibrationLaserLineCentricityItem AdaptTo()
    {
        return new CalibrationLaserLineCentricityItem
        {
            CgMicroscopeLens = MicroscopeLensInformation.LensCode == -1 ? 0 : CustomerAdaptToMapper.Mapper<MicroscopeLensInformation, CgMicroscopeLens>(MicroscopeLensInformation),
            CgMagTypeEnum = ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum(),
            Speed = ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType(),
            PmtId = PmtId,
            DarkMachineCenterPosition = DarkMachineCenterPosition.ToCgPoint(),
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredSelfCheck = IsRequiredSelfCheck
        };
    }

    #endregion Mapper
}