using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Recipe.Models.Wafer;
using Core.Recipe.Models.Wafer.ReticleMask;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Mapper.Interfaces;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Core.Recipe.Models;

public sealed partial class CalChipRecipeDTO : ObservableCacheBase, ICloneable<CalChipRecipeDTO>, IAdaptIn<CalChipRecipeDTO, CalChipRecipeDTO>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentItem))]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; }

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<CalChipSiteModelEnum, CalChipRecipeDTOItem>))]
    public ConcurrentDictionary<CalChipSiteModelEnum, CalChipRecipeDTOItem> Results { get; private set; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public CalChipRecipeDTOItem CurrentItem => Results.GetOrAdd(CalChipSiteModelEnum, _ => new CalChipRecipeDTOItem { CalChipSiteModelEnum = CalChipSiteModelEnum });

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public CalChipRecipeDTOItem DSWItem => Results.Get(CalChipSiteModelEnum.DswModel);

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public CalChipRecipeDTOItem HazeItem => Results.Get(CalChipSiteModelEnum.HazeModel);

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public CalChipRecipeDTOItem ShinyWaferItem => Results.Get(CalChipSiteModelEnum.ShinyWaferModel);

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public CalChipRecipeDTOItem UndefinedItem => Results.Get(CalChipSiteModelEnum.UndefinedModel);

    public CalChipRecipeDTO Clone() => new()
    {
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        Results = new ConcurrentDictionary<CalChipSiteModelEnum, CalChipRecipeDTOItem>
        ([
            .. Results.Select(r => new KeyValuePair<CalChipSiteModelEnum, CalChipRecipeDTOItem>(r.Key, r.Value.Clone()))
        ])
    };

    public CalChipRecipeDTO AdaptIn(CalChipRecipeDTO obj)
    {
        CalChipSiteModelEnum = obj.CalChipSiteModelEnum;
        Results = new ConcurrentDictionary<CalChipSiteModelEnum, CalChipRecipeDTOItem>
        (
        [
            .. obj.Results.Select(r => new KeyValuePair<CalChipSiteModelEnum, CalChipRecipeDTOItem>(r.Key, new CalChipRecipeDTOItem().AdaptIn(r.Value)))
        ]);
        return this;
    }
}

public sealed partial class CalChipRecipeDTOItem : ObservableCacheBase, ICloneable<CalChipRecipeDTOItem>, IAdaptIn<CalChipRecipeDTOItem, CalChipRecipeDTOItem>
{
    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; }

    /// <summary>
    /// 对准角度，用于Build Wafer
    /// </summary>
    [ObservableProperty]
    public partial double? AlignmentAbsoluteAngle { get; set; }

    [ObservableProperty]
    public partial WaferDTO CalChipMapDTO { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<ReticleMarkDTOItem> ReticleMarks { get; set; } = [];

    public CalChipRecipeDTOItem Clone() => new()
    {
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        AlignmentAbsoluteAngle = AlignmentAbsoluteAngle,
        CalChipMapDTO = CalChipMapDTO.Clone(),
        ReticleMarks = [.. ReticleMarks.Select(t => t.Clone())]
    };

    public CalChipRecipeDTOItem AdaptIn(CalChipRecipeDTOItem obj)
    {
        CalChipSiteModelEnum = obj.CalChipSiteModelEnum;
        CalChipMapDTO = new WaferDTO().AdaptIn(obj.CalChipMapDTO);
        AlignmentAbsoluteAngle = obj.AlignmentAbsoluteAngle;
        ReticleMarks = [.. obj.ReticleMarks.Select(t => new ReticleMarkDTOItem().AdaptIn(t))];
        return this;
    }
}