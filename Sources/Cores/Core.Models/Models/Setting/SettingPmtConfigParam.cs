using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Setting;

public sealed partial class SettingPmtConfigParam : ObservableObject, IAdaptIn<SettingPmtConfigParam, SettingPmtConfigParam>
{
    [ObservableProperty]
    private ObservableCollection<PmtConfigParam> _pmtConfigList = [];

    #region Mapper

    public SettingPmtConfigParam AdaptIn(SettingPmtConfigParam obj)
    {
        PmtConfigList = [.. obj.PmtConfigList.Select(x => new PmtConfigParam().AdaptIn(x))];

        return obj;
    }

    #endregion Mapper
}

public sealed partial class PmtConfigParam : ObservableObject, IAdaptIn<PmtConfigParam, PmtConfigParam>, ICloneable<PmtConfigParam>
{
    [ObservableProperty]
    private bool _enabled = true;

    [ObservableProperty]
    private int _id;

    #region Mapper

    public PmtConfigParam AdaptIn(PmtConfigParam obj)
    {
        Enabled = obj.Enabled;
        Id = obj.Id;

        return obj;
    }

    public PmtConfigParam Clone() => new()
    {
        Enabled = Enabled,
        Id = Id
    };

    #endregion Mapper
}