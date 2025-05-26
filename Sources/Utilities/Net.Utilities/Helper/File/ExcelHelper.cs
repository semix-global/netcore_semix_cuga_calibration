using MiniExcelLibs;
using System.Data;

namespace Net.Utilities.Helper.File;

/// <summary>
/// excel导入导出帮助类
/// </summary>
public static class ExcelHelper
{
    /// <summary>
    /// excel导出
    /// </summary>
    /// <param name="dataTable">导出的数据</param>
    /// <param name="excelFilePath">导出路径</param>
    /// <param name="sheetName">sheet名称</param>
    public static void DataTableToExcel(DataTable dataTable, string excelFilePath, string sheetName)
        => MiniExcel.SaveAs(excelFilePath, dataTable, sheetName: sheetName);

    /// <summary>
    /// excel导入
    /// </summary>
    /// <param name="filePath">excel文件路径</param>
    /// <param name="sheetName">sheet名称</param>
    /// <returns>数据</returns>
    public static DataTable ReadExcelToDataTable(string filePath, string sheetName)
#pragma warning disable CS0618 // 类型或成员已过时
        => MiniExcel.QueryAsDataTable(filePath, useHeaderRow: true, sheetName: sheetName);

#pragma warning restore CS0618 // 类型或成员已过时
}