using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.Cache.Providers.Bases;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

[CacheVersion("2.0.0")]
public sealed partial class FourierSideChannelFlexibleApertureDTO : CalibrationDTOBase<FourierSideChannelFlexibleApertureDTO>, IDisposable
{
    [ObservableProperty]
    public partial FourierSideChannelFlexibleApertureDTOItem Channel1Item { get; set; } = new() { ChannelId = 1 };

    [ObservableProperty]
    public partial FourierSideChannelFlexibleApertureDTOItem Channel2Item { get; set; } = new() { ChannelId = 2 };

    #region Mapper

    public override FourierSideChannelFlexibleApertureDTO Clone() => new()
    {
        Channel1Item = Channel1Item.Clone(),
        Channel2Item = Channel2Item.Clone(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    #endregion Mapper

    public void Dispose()
    {
        Channel1Item.Dispose();
        Channel2Item.Dispose();
    }
}