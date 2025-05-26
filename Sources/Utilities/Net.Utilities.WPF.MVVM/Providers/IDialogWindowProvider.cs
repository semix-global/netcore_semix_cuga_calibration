using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;

namespace Net.Utilities.WPF.MVVM.Providers;

public interface IDialogWindowProvider
{
    /// <summary>
    /// 显示弹窗并获取结果
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="message">信息</param>
    /// <param name="dialogResultEnum">结果</param>
    /// <param name="dialogButtonsEnum">按钮组</param>
    /// <param name="dialogIconEnum">图标组</param>
    /// <returns>结果</returns>
    bool? TryShowDialog(string title, string message, out DialogResultEnum dialogResultEnum, DialogButtonsEnum dialogButtonsEnum = DialogButtonsEnum.OK, DialogIconEnum dialogIconEnum = DialogIconEnum.Information);

    /// <summary>
    /// 显示弹窗并获取结果
    /// </summary>
    /// <param name="message">信息</param>
    /// <param name="dialogResultEnum">结果</param>
    /// <param name="dialogButtonsEnum">按钮组</param>
    /// <param name="dialogIconEnum">图标组</param>
    /// <returns>结果</returns>
    bool? TryShowDialog(string message, out DialogResultEnum dialogResultEnum, DialogButtonsEnum dialogButtonsEnum = DialogButtonsEnum.OK, DialogIconEnum dialogIconEnum = DialogIconEnum.Information);

    /// <summary>
    /// 显示弹窗
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="message">信息</param>
    /// <param name="dialogButtonsEnum">按钮组</param>
    /// <param name="dialogIconEnum">图标组</param>
    void ShowDialog(string title, string message, DialogButtonsEnum dialogButtonsEnum = DialogButtonsEnum.OK, DialogIconEnum dialogIconEnum = DialogIconEnum.Information);

    /// <summary>
    /// 显示弹窗
    /// </summary>
    /// <param name="message">信息</param>
    /// <param name="dialogButtonsEnum">按钮组</param>
    /// <param name="dialogIconEnum">图标组</param>
    void ShowDialog(string message, DialogButtonsEnum dialogButtonsEnum = DialogButtonsEnum.OK, DialogIconEnum dialogIconEnum = DialogIconEnum.Information);

    /// <summary>
    /// 显示弹窗
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="message">信息</param>
    /// <param name="dialogButtonsEnum">按钮组</param>
    /// <param name="dialogIconEnum">图标组</param>
    void Show(string title, string message, DialogButtonsEnum dialogButtonsEnum = DialogButtonsEnum.OK, DialogIconEnum dialogIconEnum = DialogIconEnum.Information);

    /// <summary>
    /// 显示弹窗
    /// </summary>
    /// <param name="message">信息</param>
    /// <param name="dialogButtonsEnum">按钮组</param>
    /// <param name="dialogIconEnum">图标组</param>
    void Show(string message, DialogButtonsEnum dialogButtonsEnum = DialogButtonsEnum.OK, DialogIconEnum dialogIconEnum = DialogIconEnum.Information);

    /// <summary>
    /// 显示通知
    /// </summary>
    /// <param name="message">信息</param>
    /// <param name="dialogIconEnum">图标组</param>
    /// <param name="waitSeconds">等待秒数</param>
    void ShowNotification(string message, DialogIconEnum dialogIconEnum = DialogIconEnum.Information, int waitSeconds = 3);

    /// <summary>
    /// 显示图片
    /// </summary>
    /// <param name="filePath">图片路径</param>
    void ShowImage(string filePath);

    /// <summary>
    /// 显示图片
    /// </summary>
    /// <param name="filePath">图片路径</param>
    void ShowImage(List<(string path, string title)> filePath);

    /// <summary>
    /// 显示绘图
    /// </summary>
    /// <param name="plots">绘图列表</param>
    /// <param name="title">可选标题</param>
    void ShowPlot(double[] plots, string? title = null);

    /// <summary>
    /// 显示绘图
    /// </summary>
    /// <param name="plots">绘图列表</param>
    /// <param name="title">可选标题</param>
    void ShowPlot(Point[] plots, string? title = null);

    /// <summary>
    /// 显示绘图
    /// </summary>
    /// <param name="plots">绘图列表</param>
    void ShowPlot(List<double[]> plots);

    /// <summary>
    /// 显示绘图
    /// </summary>
    /// <param name="plots">绘图列表</param>
    void ShowPlot(List<Point[]> plots);

    /// <summary>
    /// 显示绘图
    /// </summary>
    /// <param name="plots">绘图列表</param>
    void ShowPlot(List<(string Title, double[] Points)> plots);

    /// <summary>
    /// 显示绘图
    /// </summary>
    /// <param name="plots">绘图列表</param>
    void ShowPlot(List<(string Title, Point[] Points)> plots);

    /// <summary>
    /// 选取单个文件
    /// </summary>
    /// <param name="filter">过滤器(.shm or .jpg or .png)</param>
    /// <param name="filePath">输出文件路径</param>
    bool? TryShowSelectFilePathDialog(string filter, out string filePath);

    /// <summary>
    /// 选择文件夹
    /// </summary>
    /// <param name="directoryPath">输出文件夹路径</param>
    bool? TryShowSelectDirectoryPathDialog(out string directoryPath);

    /// <summary>
    /// 保存单个文件
    /// </summary>
    /// <param name="filter">过滤器(.shm or .jpg or .png)</param>
    /// <param name="filePath">输出文件路径</param>
    bool? TryShowSaveFilePathDialog(string filter, out string filePath);
}