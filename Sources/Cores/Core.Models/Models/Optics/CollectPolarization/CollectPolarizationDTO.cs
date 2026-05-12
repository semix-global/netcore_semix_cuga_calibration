using CommunityToolkit.Mvvm.ComponentModel;
using Core.Wcf.Models.Laser;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;


namespace Core.Models.Models.Optics.CollectPolarization;

[CacheVersion("1.0.0")]
public sealed partial class CollectPolarizationDTO : CalibrationDTOBase<CollectPolarizationDTO>, IAdaptTo<CalibrationCollectionPolarization>
{
    [ObservableProperty]
    private double _polarizationPositionNDFSCH1;

    [ObservableProperty]
    private double _polarizationPositionNDFSCH2;

    [ObservableProperty]
    private double _polarizationPositionNDFSCH3;

    [ObservableProperty]
    private double _polarizationPositionNDFPCH1;

    [ObservableProperty]
    private double _polarizationPositionNDFPCH2;

    [ObservableProperty]
    private double _polarizationPositionNDFPCH3;

    #region Mapper

    public override CollectPolarizationDTO Clone() => new()
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