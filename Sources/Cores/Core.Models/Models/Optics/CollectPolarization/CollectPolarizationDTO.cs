using CommunityToolkit.Mvvm.ComponentModel;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;


namespace Core.Models.Models.Optics.CollectPolarization;


public sealed partial class CollectPolarizationDTO : CalibrationDtoBase, ICloneable<CollectPolarizationDTO>, IAdaptTo<CalibrationCollectionPolarization>
{
    [ObservableProperty]
    private double _polarizationPositionNDFSCH1 = 0;

    [ObservableProperty]
    private double _polarizationPositionNDFSCH2 = 0;

    [ObservableProperty]
    private double _polarizationPositionNDFSCH3 = 0;

    [ObservableProperty]
    private double _polarizationPositionNDFPCH1 = 0;

    [ObservableProperty]
    private double _polarizationPositionNDFPCH2 = 0;

    [ObservableProperty]
    private double _polarizationPositionNDFPCH3 = 0;

    #region Mapper  
    public CollectPolarizationDTO Clone() => new()
    {
        PolarizationPositionNDFSCH1 = PolarizationPositionNDFSCH1,
        PolarizationPositionNDFSCH2 = PolarizationPositionNDFSCH2,
        PolarizationPositionNDFSCH3 = PolarizationPositionNDFSCH3,
        PolarizationPositionNDFPCH1 = PolarizationPositionNDFPCH1,
        PolarizationPositionNDFPCH2 = PolarizationPositionNDFPCH2,
        PolarizationPositionNDFPCH3 = PolarizationPositionNDFPCH3,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationCollectionPolarization AdaptTo() => new()
    {
        PolarizationPositionNDFSCH1 = PolarizationPositionNDFSCH1,
        PolarizationPositionNDFSCH2 = PolarizationPositionNDFSCH2,
        PolarizationPositionNDFSCH3 = PolarizationPositionNDFSCH3,
        PolarizationPositionNDFPCH1 = PolarizationPositionNDFPCH1,
        PolarizationPositionNDFPCH2 = PolarizationPositionNDFPCH2,
        PolarizationPositionNDFPCH3 = PolarizationPositionNDFPCH3,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}
