using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.Cache.Providers.Bases;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

[CacheVersion("2.0.0")]
public sealed partial class FourierSideChannelFlexibleApertureDTO : CalibrationDTOBase<FourierSideChannelFlexibleApertureDTO>, IDisposable
{
    [Newtonsoft.Json.JsonProperty]
    private readonly int _rodTotalCount;

    [ObservableProperty]
    public partial FourierSideChannelFlexibleApertureDTOItem Channel1Item { get; set; }

    [ObservableProperty]
    public partial FourierSideChannelFlexibleApertureDTOItem Channel2Item { get; set; }

    public FourierSideChannelFlexibleApertureDTO()
    {
    }

    public FourierSideChannelFlexibleApertureDTO(int rodTotalCount)
    {
        _rodTotalCount = rodTotalCount;

        Channel1Item = new FourierSideChannelFlexibleApertureDTOItem(rodTotalCount) { ChannelId = 1 };
        Channel2Item = new FourierSideChannelFlexibleApertureDTOItem(rodTotalCount) { ChannelId = 2 };
    }

    #region Mapper

#pragma warning disable IDISP003

    public override FourierSideChannelFlexibleApertureDTO Clone() => new(_rodTotalCount)
    {
        Channel1Item = Channel1Item.Clone(),
        Channel2Item = Channel2Item.Clone(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

#pragma warning restore IDISP003

    #endregion Mapper

    public void Dispose()
    {
        Channel1Item.Dispose();
        Channel2Item.Dispose();
    }
}