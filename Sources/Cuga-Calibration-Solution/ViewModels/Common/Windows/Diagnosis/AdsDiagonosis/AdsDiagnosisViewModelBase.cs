using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniExcelLibs;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Behaviors;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;

namespace CugaCalibration.ViewModels.Common.Windows.Diagnosis.AdsDiagonosis;

[IOCAppService(ServiceType = typeof(AdsDiagnosisViewModelBase), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class AdsDiagnosisViewModelBase(
    IDialogWindowProvider dialogWindowProvider,
    ILogger<AdsDiagnosisViewModelBase> logger,
    IOptions<ApplicationSetting> options) : ViewModelBase
{
    #region 属性

    /// <summary>
    /// 日志唯一标识
    /// </summary>
    public Guid HtmlLogUniqueId { get; internal set; }

    /// <summary>
    /// Csv文件存储名称前缀
    /// </summary>
    public string CsvFileDirectory => Path.Combine(options.Value.AppHomeDirectory, "Csv", "Diagnosis");

    /// <summary>
    /// Csv文件存储名称前缀
    /// </summary>
    public virtual string LogHtmlFileName { get; set; } = string.Empty;

    /// <summary>
    /// 校准文件名称
    /// </summary>
    public string DiagnosisHtmlLogFileName => string.IsNullOrWhiteSpace(LogHtmlFileName) ? "Diagnosis" : $"Diagnosis-{FileHelper.RemoveInvalidFileName(LogHtmlFileName)}";

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;

    /// <summary>
    /// tracebuffer监测时间
    /// </summary>
    [ObservableProperty]
    private int _waitTime = 5;

    /// <summary>
    /// 诊断对象集合所有tracebuffer数据，用于保存到csv文件
    /// </summary>
    public List<List<WpfPlotModel>> TracebufferList = [];

    #region 界面

    [ObservableProperty]
    private bool _isEnableWindow = true;

    /// <summary>
    /// tracebuffer界面显示
    /// </summary>
    [ObservableProperty]
    private List<WpfPlotModel> _plotListZ = [];

    #endregion 界面

    #endregion 属性

    #region Command

    public void ImportConfig()
    {
        var dialog = dialogWindowProvider.TryShowSelectFilePathDialog(".xlsx", out var filePath);
        if (dialog == false) return;
        if (ImportingConfig(filePath) == false)
            dialogWindowProvider.ShowDialog("Import Config Failed! Please Check Config File Format And Try Again!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
    }

    public async Task ActionAsync(CancellationToken cancellationToken)
    {
        logger.LogHtmlInformation($"1. {LogHtmlFileName}", HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
        PlotListZ.Clear();
        TracebufferList.Clear();
        var result = true;
        if (await DiagnosisActionAsync(cancellationToken).ConfigureAwait(false) == false)
        {
            dialogWindowProvider.ShowDialog("Diagnosis Action Failed! ", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            result = false;
        }

        logger.LogHtmlInformation(HtmlLogUniqueId.LoggingPeekHtml($"{DiagnosisHtmlLogFileName}_{(result ? "OK" : "Failed")}"));
        logger.LogHtmlInformation(HtmlLogUniqueId.LoggingClearHtml());
        HtmlLogUniqueId = Guid.NewGuid();
    }

    public async Task SaveAsync()
    {
        await Task.Run(() =>
        {
            var directorypath = Path.Combine(CsvFileDirectory, LogHtmlFileName);
            var isSuccess = SaveAsExcel(TracebufferList, directorypath);
            dialogWindowProvider.ShowDialog($"Save Csv {(isSuccess ? "Success" : "Failed")}! ", DialogButtonsEnum.OK, isSuccess ? DialogIconEnum.Information : DialogIconEnum.Warning);
        }).ConfigureAwait(false);
    }

    #endregion Command

    #region 重载

    public virtual Task LoadingAsync() => Task.CompletedTask;

    public virtual bool ImportingConfig(string filePath) => true;

    public virtual Task<bool> DiagnosisActionAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    #endregion 重载

    #region 文件读写

    /// <summary>
    /// 存储数据到Excel文件中
    /// </summary>
    /// <param name="tracebuffersList">tracebuffer曲线的集合</param>
    /// <param name="directoryPath"></param>
    /// <returns></returns>
    private bool SaveAsExcel(List<List<WpfPlotModel>> tracebuffersList, string directoryPath)
    {
        try
        {
            DirectoryHelper.CreateDirectoryIfNotExists(directoryPath);
            var csvPath = Path.Combine(directoryPath, DateTime.Now.ToString("yyyyMMddHHmmss")) + ".csv";
            var resultList = new List<(string title, List<double> buffers)>();
            //插入X轴数据
            var xList = tracebuffersList.First().First().Points.Select(t => t.X).ToList();
            resultList.Add(("X", xList));
            // 将数据转换为扁平化格式，适应 Excel 的表格结构
            var buffersList = tracebuffersList.Select((t, i) => (index: i, buffers: t)).SelectMany(parentList =>
                parentList.buffers.Select(t => ($"{t.Title}({parentList.index})", t.Points.Select(p => p.Y).ToList())
                ).ToList()
            ).ToList();
            resultList.AddRange(buffersList);

            // 获取最小行数（会偶尔出现最后缺一个buffer的情况）
            var minRows = resultList.Min(r => r.buffers.Count);

            // 创建写入Excel的数据结构
            var values = new List<Dictionary<string, object>>();

            // 将每一行的数据作为字典的一个元素
            for (var i = 0; i < minRows; i++)
            {
                var row = new Dictionary<string, object>();
                foreach (var (title, buffers) in resultList)
                {
                    if (i <= buffers.Count)
                    {
                        row[title] = buffers[i]; // 防止索引越界
                    }
                }

                values.Add(row);
            }

            MiniExcel.SaveAs(csvPath, values);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name} Save As Excel Failed! FileName: {@DirectoryPath}", nameof(AdsGainsDiagnosisViewModel), directoryPath);
            return false;
        }
    }

    #endregion 文件读写
}