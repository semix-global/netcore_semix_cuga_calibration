using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Cuga.Data.DataStruct.PMT;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace Core.Models.Models.Common.DarkField;

public sealed partial class CIBDelayDTO : ObservableObject, ICloneable<CIBDelayDTO>, IAdaptTo<CgPMTDelayModel>, IAdaptIn<CgPMTDelayModel, CIBDelayDTO>
{
    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial double PMTDelay { get; set; }

    [ObservableProperty]
    public partial double SenseDelay { get; set; }

    [ObservableProperty]
    public partial double AGCDelay { get; set; }

    public CIBDelayDTO WithPMTDelayAndSenseDelay(double pmtDelay)
    {
        PMTDelay = pmtDelay;
        SenseDelay = pmtDelay;

        return this;
    }

    public CIBDelayDTO WithAGCDelay(double agcDelay)
    {
        AGCDelay = agcDelay;

        return this;
    }

    #region Mapper

    public CIBDelayDTO Clone() => new()
    {
        CIBInformation = CIBInformation.Clone(),
        PMTDelay = PMTDelay,
        SenseDelay = SenseDelay,
        AGCDelay = AGCDelay
    };

    public CgPMTDelayModel AdaptTo() => new()
    {
        PMTId = CIBInformation.PMTId,
        Channel = CIBInformation.ChannelId,
        PMTDelay = (int)PMTDelay,
        SenseDelay = (int)SenseDelay,
        DAC_Delay = (int)AGCDelay
    };

    public CIBDelayDTO AdaptIn(CgPMTDelayModel obj)
    {
        CIBInformation = CIBInformation.Default.Clone().AdaptIn((obj.PMTId, obj.Channel, true));
        PMTDelay = obj.PMTDelay;
        SenseDelay = obj.SenseDelay;
        AGCDelay = obj.DAC_Delay;

        return this;
    }

    #endregion Mapper

    public object ToHtmlAnonymous() => new
    {
        CIBInformation = new HtmlQuote(CIBInformation.ToHtmlAnonymous()),
        PMTDelay,
        SenseDelay,
        AGCDelay
    };
}