using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.LineOrientationOffset;

public sealed partial class LineOrientationOffsetItemDto : CalibrationDtoBase, ICloneable<LineOrientationOffsetItemDto>, IAdaptTo<CalibrationLaserLineOrientationOffsetItem>
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private StageSpeedEnum _stageSpeedEnum;

    [ObservableProperty]
    private int _pmtId;

    [ObservableProperty]
    private Point _startPosition;

    [ObservableProperty]
    private Point _endPosition;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private Point _forwardFindDarkMachinePosition;

    [ObservableProperty]
    private Point _reverseFindDarkMachinePosition;

    public Point Offset => ReverseFindDarkMachinePosition - (Vector)ForwardFindDarkMachinePosition;

    [ObservableProperty]
    private string _forwardFilePath = string.Empty;

    [ObservableProperty]
    private string _reverseFilePath = string.Empty;

    [ObservableProperty]
    private string _templateFilePath = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath = string.Empty;

    #region Mapper

    public LineOrientationOffsetItemDto Clone()
    {
        return new LineOrientationOffsetItemDto
        {
            MicroscopeLensInformation = MicroscopeLensInformation,
            OpticsMagTypeEnum = OpticsMagTypeEnum,
            StageSpeedEnum = StageSpeedEnum,
            PmtId = PmtId,
            FindPosition = FindPosition,
            StartPosition = StartPosition,
            EndPosition = EndPosition,
            ForwardFindDarkMachinePosition = ForwardFindDarkMachinePosition,
            ReverseFindDarkMachinePosition = ReverseFindDarkMachinePosition,
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

    public CalibrationLaserLineOrientationOffsetItem AdaptTo()
    {
        return new CalibrationLaserLineOrientationOffsetItem
        {
            CgMicroscopeLens = MicroscopeLensInformation.LensCode == -1 ? 0 : CustomerAdaptToMapper.Mapper<MicroscopeLensInformation, CgMicroscopeLens>(MicroscopeLensInformation),
            CgMagTypeEnum = OpticsMagTypeEnum.ToCgMagTypeEnum(),
            Speed = StageSpeedEnum.ToCgSpeedLevelType(),
            PmtId = PmtId,
            Offset = Offset.ToCgPoint(),
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredCalibrate = IsRequiredSelfCheck
        };
    }

    #endregion Mapper
}