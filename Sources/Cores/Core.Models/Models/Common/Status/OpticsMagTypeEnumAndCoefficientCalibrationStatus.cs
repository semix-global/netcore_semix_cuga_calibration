using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Local.NoSQL.DB.Providers.Bases;
using System.ComponentModel;

namespace Core.Models.Models.Common.Status;

public sealed partial class OpticsMagTypeEnumAndCoefficientCalibrationStatus : ObservableCacheBase
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private BindingList<CoefficientCalibrationStatus> _coefficientList = [];

    public bool IsCalibrated => CoefficientList.All(c => c.IsCalibrated);

    partial void OnCoefficientListChanged(BindingList<CoefficientCalibrationStatus>? oldValue, BindingList<CoefficientCalibrationStatus> newValue)
    {
        if (oldValue != null) oldValue.ListChanged -= OnValueOnListChanged;

        newValue.ListChanged += OnValueOnListChanged;
    }

    private void OnValueOnListChanged(object o, ListChangedEventArgs listChangedEventArgs)
    {
        OnPropertyChanged(nameof(IsCalibrated));
    }
}

public sealed partial class CoefficientCalibrationStatus : ObservableCacheBase
{
    [ObservableProperty]
    private double _coefficient;

    [ObservableProperty]
    private bool _isCalibrated;

    public static List<CoefficientCalibrationStatus> CreateList()
    {
        var list = CalibrationConstantsHelper.CalibrationCoefficients.Select(tt =>
        {
            var coefficientStatus = new CoefficientCalibrationStatus { Coefficient = tt, IsCalibrated = false };
            if (coefficientStatus.Coefficient - 1 == 0) coefficientStatus.IsCalibrated = true;

            return coefficientStatus;
        }).ToList();

        return list;
    }
}