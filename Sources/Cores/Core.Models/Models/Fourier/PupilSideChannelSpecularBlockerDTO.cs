using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Fourier;
using Cuga.Data.DataStruct.Stage;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Core.Models.Models.Fourier;


public sealed partial class PupilSideChannelSpecularBlockerDTO : CalibrationDtoBase, ICloneable<PupilSideChannelSpecularBlockerDTO>, IAdaptTo<CalibrationPupilSideChannelSpecularBlocker>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationMode = OpticsIlluminationModeEnum.OI;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    public Point _cgFFBoxBeginPositionCh1 = Point.Origin;

    [ObservableProperty]
    public Point _cgFFBoxBeginPositionCh2 = Point.Origin;

    [ObservableProperty]
    public int _cgFFBoxBeginNumberCh1 = 1;

    [ObservableProperty]
    public int _cgFFBoxBeginNumberCh2 = 1;

    [ObservableProperty]
    public int _cgFFBoxEndNumberCh1 = 3;

    [ObservableProperty]
    public int _cgFFBoxEndNumberCh2 = 3;

    [ObservableProperty]
    public List<double> _cgFFBoxMoveDownPercentListCh1 = new List<double> { 0.3 };

    [ObservableProperty]
    public List<double> _cgFFBoxMoveDownPercentListCh2 = new List<double> { 0.3 };

    #region Mapper

    public PupilSideChannelSpecularBlockerDTO Clone()
    {
        return new PupilSideChannelSpecularBlockerDTO
        {
            OpticsIlluminationMode = OpticsIlluminationMode,   
            ProductivityInformation = ProductivityInformation.Clone(),
            CgFFBoxBeginPositionCh1 = CgFFBoxBeginPositionCh1,
            CgFFBoxBeginPositionCh2 = CgFFBoxBeginPositionCh2,
            CgFFBoxBeginNumberCh1 = CgFFBoxBeginNumberCh1,
            CgFFBoxBeginNumberCh2 = CgFFBoxBeginNumberCh2,
            CgFFBoxEndNumberCh1 = CgFFBoxEndNumberCh1,
            CgFFBoxEndNumberCh2 = CgFFBoxEndNumberCh2,
            CgFFBoxMoveDownPercentListCh1 = CgFFBoxMoveDownPercentListCh1,
            CgFFBoxMoveDownPercentListCh2 = CgFFBoxMoveDownPercentListCh2,

            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredSelfCheck = IsRequiredSelfCheck,
            Id = Id,
            Expiration = Expiration
        };
    }

    public CalibrationPupilSideChannelSpecularBlocker AdaptTo() => new()
    {
        CgFFBoxBeginPositionCh1 = new CgPoint((int)Math.Round(CgFFBoxBeginPositionCh1.X), (int)Math.Round(CgFFBoxBeginPositionCh1.Y)),
        CgFFBoxBeginNumberCh1 = CgFFBoxBeginNumberCh1,
        CgFFBoxEndNumberCh1 = CgFFBoxEndNumberCh1,
        CgFFBoxMoveDownPercentListCh1 = CgFFBoxMoveDownPercentListCh1,
        CgFFBoxBeginPositionCh2 = new CgPoint((int)Math.Round(CgFFBoxBeginPositionCh2.X), (int)Math.Round(CgFFBoxBeginPositionCh2.Y)),
        CgFFBoxBeginNumberCh2 = CgFFBoxBeginNumberCh2,
        CgFFBoxEndNumberCh2 = CgFFBoxEndNumberCh2,
        CgFFBoxMoveDownPercentListCh2 = CgFFBoxMoveDownPercentListCh2,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

