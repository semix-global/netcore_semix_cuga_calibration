using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.DarkField;

public sealed partial class CIBMMDGainRelationshipDTO : ObservableObject, ICloneable<CIBMMDGainRelationshipDTO>
{
    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial double Gain { get; set; }

    /// <summary>
    /// 14bitSense值, 无符号位
    /// </summary>
    [ObservableProperty]
    public partial int SenseU14Bit { get; set; }

    /// <summary>
    /// 16位增益值, 有符号位
    /// </summary>
    [ObservableProperty]
    public partial int GainS16Bit { get; set; }

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