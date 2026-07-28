using System.ComponentModel;

namespace Core.Models.Models.AOD.Uniformity;

public partial class AODUniformityDTO
{
    partial void OnStartWindowItemChanged(WindowItem oldValue, WindowItem newValue)
    {
        oldValue.PropertyChanged -= ItemOnPropertyChanged;

        newValue.PropertyChanged -= ItemOnPropertyChanged;
        newValue.PropertyChanged += ItemOnPropertyChanged;

        OnPropertyChanged(nameof(IsReverse));
        RefreshIsReversePlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsReverse));
            RefreshIsReversePlot();
        }
    }

    partial void OnStopWindowItemChanged(WindowItem oldValue, WindowItem newValue)
    {
        oldValue.PropertyChanged -= ItemOnPropertyChanged;

        newValue.PropertyChanged -= ItemOnPropertyChanged;
        newValue.PropertyChanged += ItemOnPropertyChanged;

        OnPropertyChanged(nameof(IsReverse));
        RefreshIsReversePlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsReverse));
            RefreshIsReversePlot();
        }
    }

    partial void OnMappingWindowItemChanged(WindowItem oldValue, WindowItem newValue)
    {
        oldValue.PropertyChanged -= ItemOnPropertyChanged;

        newValue.PropertyChanged -= ItemOnPropertyChanged;
        newValue.PropertyChanged += ItemOnPropertyChanged;

        RefreshIsReversePlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshIsReversePlot();
    }

    partial void OnMappingsChanged(IReadOnlyList<Mapping> oldValue, IReadOnlyList<Mapping> newValue)
    {
        foreach (var item in oldValue) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshMappingPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshMappingPlot();
    }

    partial void OnInitializeWindowItemChanged(AODUniformityDTOItem oldValue, AODUniformityDTOItem newValue)
    {
        oldValue.PropertyChanged -= ItemOnPropertyChanged;

        newValue.PropertyChanged -= ItemOnPropertyChanged;
        newValue.PropertyChanged += ItemOnPropertyChanged;

        RefreshInitializeWindowPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshInitializeWindowPlot();
    }

    partial void OnItemChanged(AODUniformityDTOItem oldValue, AODUniformityDTOItem newValue)
    {
        oldValue.PropertyChanged -= ItemOnPropertyChanged;

        newValue.PropertyChanged -= ItemOnPropertyChanged;
        newValue.PropertyChanged += ItemOnPropertyChanged;

        RefreshPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshPlot();
    }

    partial void OnItemsChanged(IReadOnlyList<AODUniformityDTOItem> oldValue, IReadOnlyList<AODUniformityDTOItem> newValue)
    {
        foreach (var item in oldValue) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshPlots();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshPlots();
    }
}