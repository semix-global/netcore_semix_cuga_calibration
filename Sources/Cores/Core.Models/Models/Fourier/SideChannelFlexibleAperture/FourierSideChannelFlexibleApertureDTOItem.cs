using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.OpticsFourierImageViewer.WPF;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

public sealed partial class FourierSideChannelFlexibleApertureDTOItem : ObservableObject, ICloneable<FourierSideChannelFlexibleApertureDTOItem>, IDisposable
{
    [ObservableProperty]
    public partial int ChannelId { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial OpticsFourierImageDocument Document { get; set; }

    public FourierSideChannelFlexibleApertureDTOItem()
    {
        Document = new OpticsFourierImageDocument();
    }


    public FourierSideChannelFlexibleApertureDTOItem Clone() => new()
    {
        ChannelId = ChannelId
    };

    public void Dispose()
    {
    }
}