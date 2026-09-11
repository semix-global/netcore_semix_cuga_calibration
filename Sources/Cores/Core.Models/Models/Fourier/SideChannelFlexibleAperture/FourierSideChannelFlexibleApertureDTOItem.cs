using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.OpticsFourierImageViewer.WPF;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

public sealed partial class FourierSideChannelFlexibleApertureDTOItem : ObservableObject, ICloneable<FourierSideChannelFlexibleApertureDTOItem>, IDisposable
{
    [Newtonsoft.Json.JsonProperty]
    private readonly int _rodTotalCount;

    [ObservableProperty]
    public partial int ChannelId { get; set; }

    [ObservableProperty]
    public partial Item EvenItem { get; set; }

    [ObservableProperty]
    public partial Item OddItem { get; set; }

    public FourierSideChannelFlexibleApertureDTOItem(int rodTotalCount)
    {
        _rodTotalCount = rodTotalCount;

        EvenItem = new Item(rodTotalCount, true);
        OddItem = new Item(rodTotalCount, false);
    }

#pragma warning disable IDISP003

    public FourierSideChannelFlexibleApertureDTOItem Clone() => new(_rodTotalCount)
    {
        ChannelId = ChannelId,
        EvenItem = EvenItem.Clone(),
        OddItem = OddItem.Clone()
    };

#pragma warning restore IDISP003


    public void Dispose()
    {
        EvenItem.Dispose();
        OddItem.Dispose();
    }
}