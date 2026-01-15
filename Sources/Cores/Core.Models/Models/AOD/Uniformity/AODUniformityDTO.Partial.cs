using System.ComponentModel;

namespace Core.Models.Models.AOD.Uniformity;

public partial class AODUniformityDTO
{
    partial void OnStartWindowItemChanged(WindowItem? oldValue, WindowItem newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= ItemOnPropertyChanged;

        newValue.PropertyChanged -= ItemOnPropertyChanged;
        newValue.PropertyChanged += ItemOnPropertyChanged;

        RefreshIsReversePlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshIsReversePlot();
    }

    partial void OnStopWindowItemChanged(WindowItem? oldValue, WindowItem newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= ItemOnPropertyChanged;

        newValue.PropertyChanged -= ItemOnPropertyChanged;
        newValue.PropertyChanged += ItemOnPropertyChanged;

        RefreshIsReversePlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshIsReversePlot();
    }

    partial void OnMappingWindowItemChanged(WindowItem? oldValue, WindowItem newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= ItemOnPropertyChanged;

        newValue.PropertyChanged -= ItemOnPropertyChanged;
        newValue.PropertyChanged += ItemOnPropertyChanged;

        RefreshIsReversePlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshIsReversePlot();
    }

    partial void OnMappingsChanged(IReadOnlyList<Mapping>? oldValue, IReadOnlyList<Mapping> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshMappingPlot();
    }

    partial void OnItemChanged(AODUniformityDTOItem? oldValue, AODUniformityDTOItem newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= ItemOnPropertyChanged;

        newValue.PropertyChanged -= ItemOnPropertyChanged;
        newValue.PropertyChanged += ItemOnPropertyChanged;

        RefreshInitializeWindowPlot();
        RefreshPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AODUniformityDTOItem.InitializeWindowItems):
                    RefreshInitializeWindowPlot();

                    break;

                case nameof(AODUniformityDTOItem.Items):
                    RefreshPlot();

                    break;

                default:
                    RefreshInitializeWindowPlot();
                    RefreshPlot();

                    break;
            }
        }
    }

    partial void OnItemsChanged(IReadOnlyList<AODUniformityDTOItem>? oldValue, IReadOnlyList<AODUniformityDTOItem> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshPlot();
    }
}