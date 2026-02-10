using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

#if NETFRAMEWORK
using Cuga.Data.DataStruct.PMT;
#endif

namespace Core.Models.Models.Common.DarkField;

public sealed partial class CIBMMDGainRelationshipDTO : ObservableCacheBase, ICloneable<CIBMMDGainRelationshipDTO>
{
    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private double _gain;

    /// <summary>
    /// 14bitSense值, 无符号位
    /// </summary>
    [ObservableProperty]
    private int _senseU14Bit;

    /// <summary>
    /// 16位增益值, 有符号位
    /// </summary>
    [ObservableProperty]
    private int _gainS16Bit;

    public CIBMMDGainRelationshipDTO Clone() => new()
    {
        CIBInformation = CIBInformation.Clone(),
        Gain = Gain,
        SenseU14Bit = SenseU14Bit,
        GainS16Bit = GainS16Bit
    };

    public CIBMMDGainRelationshipDTO WithCIBInformation(CIBInformation cibInformation)
    {
        CIBInformation = cibInformation;

        return this;
    }

    public CIBMMDGainRelationshipDTO WithGain(double gain)
    {
        Gain = gain;

        return this;
    }

    public CIBMMDGainRelationshipDTO WithSenseU14Bit(int senseU14Bit)
    {
        SenseU14Bit = senseU14Bit;

        return this;
    }

    public CIBMMDGainRelationshipDTO WithGainS16Bit(int gainS16Bit)
    {
        GainS16Bit = gainS16Bit;

        return this;
    }
}