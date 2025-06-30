using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.LineCentricity;

public sealed partial class LaserLineCentricityItemDto : CalibrationDtoBase, ICloneable<LaserLineCentricityItemDto>, IAdaptTo<CalibrationLaserLineCentricityItem>
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private StageSpeedEnum _stageSpeedEnum;

    [ObservableProperty]
    private int _pmtId;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private Point _findBrightMachinePosition;

    [ObservableProperty]
    private Point _forwardFindDarkMachinePosition;

    [ObservableProperty]
    private Point _forwardDarkMachineCenterPosition;

    [ObservableProperty]
    private Point _reverseFindDarkMachinePosition;

    [ObservableProperty]
    private Point _reverseDarkMachineCenterPosition;

    [ObservableProperty]
    private string _forwardFilePath = string.Empty;

    [ObservableProperty]
    private string _reverseFilePath = string.Empty;

    [ObservableProperty]
    private string _templateFilePath = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath = string.Empty;

    #region Mapper

    public LaserLineCentricityItemDto Clone()
    {
        return new LaserLineCentricityItemDto
        {
            MicroscopeMagnificationEnum = MicroscopeMagnificationEnum,
            OpticsMagTypeEnum = OpticsMagTypeEnum,
            StageSpeedEnum = StageSpeedEnum,
            PmtId = PmtId,
            FindPosition = FindPosition,
            FindBrightMachinePosition = FindBrightMachinePosition,
            ForwardFindDarkMachinePosition = ForwardFindDarkMachinePosition,
            ForwardDarkMachineCenterPosition = ForwardDarkMachineCenterPosition,
            ReverseFindDarkMachinePosition = ReverseFindDarkMachinePosition,
            ReverseDarkMachineCenterPosition = ReverseDarkMachineCenterPosition,
            ForwardFilePath = ForwardFilePath,
            ReverseFilePath = ReverseFilePath,
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
            CgMicroscopeLens = CustomerAdaptToMapper.Mapper<MicroscopeMagnificationEnum, CgMicroscopeLens>(MicroscopeMagnificationEnum),
            CgMagTypeEnum = OpticsMagTypeEnum.ToCgMagTypeEnum(),
            Speed = StageSpeedEnum.ToAdsSpeedEnum(),
            PmtId = PmtId,
            DarkMachineCenterPosition = ForwardDarkMachineCenterPosition.ToCgPoint(),
            ReverseDarkMachineCenterPosition = ReverseDarkMachineCenterPosition.ToCgPoint(),
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredSelfCheck = IsRequiredSelfCheck
        };
    }

    #endregion Mapper
}