using CugaCalibration.ViewModels.Laser;
using Net.Utilities.Helper.File;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CugaCalibration.Views.Laser.PmtGain.Children;

public sealed partial class DataView
{
    private Dictionary<double, Dictionary<int, double>> laserPmtGainDic = [];
    private string pmtName = string.Empty;
    private bool _isLoaded;

    public DataView()
    {
        InitializeComponent();

        Loaded -= OnLoaded;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;

        if (DataContext is not LaserPmtGainCalibrationViewModel viewModel) return;
        viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (sender is not LaserPmtGainCalibrationViewModel viewModel) return;

        switch (e.PropertyName)
        {
            case nameof(viewModel.SelectLaserPmtGainDto):
                Dispatcher.Invoke(() =>
                {
                    laserPmtGainDic = [];
                    var selectLaserPmtGainDto = viewModel.SelectLaserPmtGainDto;
                    pmtName = "PMT" + selectLaserPmtGainDto?.PmtId.ToString() + "_Channel" + selectLaserPmtGainDto?.Channel.ToString();
                    var pmtNameN = "PMT:" + selectLaserPmtGainDto?.PmtId.ToString() + " Channel:" + selectLaserPmtGainDto?.Channel.ToString();
                    groupBox.Header = pmtNameN;
                    if (selectLaserPmtGainDto != null)
                    {
                        foreach (var itemPmtGainDto in selectLaserPmtGainDto.Plot)
                        {
                            foreach (var item in itemPmtGainDto.VoltageLightListPoint)
                            {
                                if (laserPmtGainDic.TryGetValue(item.X, out var valueDic))
                                {
                                    if (!valueDic.ContainsKey(int.Parse(itemPmtGainDto.MeasurePower)))
                                    {
                                        valueDic.Add(int.Parse(itemPmtGainDto.MeasurePower), item.Y);
                                    }

                                    laserPmtGainDic[item.X] = valueDic;
                                }
                                else
                                {
                                    var dic = new Dictionary<int, double>
                                    {
                                        { int.Parse(itemPmtGainDto.MeasurePower), item.Y }
                                    };
                                    laserPmtGainDic.Add(item.X, dic);
                                }
                            }
                        }
                    }

                    var data = laserPmtGainDic;
                    DataListView.Items.Clear();
                    GridViewControl.Columns.Clear();
                    if (data.Count <= 0) return;

                    var servingColumn = new GridViewColumn
                    {
                        Header = "Voltage",
                        Width = 70,
                        DisplayMemberBinding = new Binding("[0]")
                    };
                    GridViewControl.Columns.Add(servingColumn);

                    var coefficients = data.SelectMany(d => d.Value.Keys).Distinct().OrderBy(k => k).ToList();

                    for (var i = 0; i < coefficients.Count; i++)
                    {
                        var coefficientColumn = new GridViewColumn
                        {
                            Header = $"{coefficients[i]}",
                            Width = 60,
                            DisplayMemberBinding = new Binding($"[{i + 1}]")
                        };
                        GridViewControl.Columns.Add(coefficientColumn);
                    }

                    foreach (var serving in data.Keys)
                    {
                        var row = new List<string> { $"{serving}" };
                        row.AddRange(coefficients
                            .Select(coefficient => data[serving].TryGetValue(coefficient, out var value) ? $"{value:f2}" : "-"));

                        DataListView.Items.Add(row);
                    }
                });
                break;
        }
    }

    private void ExcelExport_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LaserPmtGainCalibrationViewModel viewModel) return;
        var path = Path.Combine(viewModel.TemplateFileDirectory, $"{pmtName}.xlsx");
        ExcelExport(path);
    }

    private void ExcelTempExport_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LaserPmtGainCalibrationViewModel viewModel) return;
        var pathFile = Path.Combine(viewModel.TemplateFileDirectory, "Reference");
        if (!File.Exists(pathFile))
        {
            Directory.CreateDirectory(pathFile);
        }

        var path = Path.Combine(pathFile, $"{pmtName}.xlsx");
        ExcelExport(path);
    }

    private void ExcelExport(string path)
    {
        using var dataTable = new DataTable();
        var data = laserPmtGainDic;

        var servingColumn = new DataColumn
        {
            DataType = typeof(string),
            ColumnName = "Voltage"
        };
        dataTable.Columns.Add(servingColumn);
        var coefficients = data.SelectMany(d => d.Value.Keys).Distinct().OrderBy(k => k).ToList();

        for (var i = 0; i < coefficients.Count; i++)
        {
            var coefficientColumn = new DataColumn
            {
                DataType = typeof(string),
                ColumnName = $"{coefficients[i]}"
            };
            dataTable.Columns.Add(coefficientColumn);
        }

        foreach (var serving in data.Keys)
        {
            var dr = dataTable.NewRow();
            var row = new List<string> { $"{serving}" };
            row.AddRange(coefficients
                .Select(coefficient => data[serving].TryGetValue(coefficient, out var value) ? $"{value:f2}" : "-"));
            for (var i = 0; i < row.Count; i++)
            {
                dr[i] = row[i];
            }

            dataTable.Rows.Add(dr);
        }

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        ExcelHelper.DataTableToExcel(dataTable, path, "sheetName1");
    }
}