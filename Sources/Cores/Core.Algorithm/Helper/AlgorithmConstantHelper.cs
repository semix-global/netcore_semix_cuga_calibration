using Core.Models.Helper;
using Net.Utilities.Helpers.Helpers.Files;

namespace Core.Algorithm.Helper;

internal static class AlgorithmConstantHelper
{
    public static string TemplatePathToTemplateOriginImagePath(string templateFilePath)
    {
        var originImagePath = CalibrationConstantsHelper.TemplatePathToOriginImagePath(templateFilePath);
        DirectoryHelper.CreateFileDirectoryIfNotExists(originImagePath);
        FileHelper.DeleteFileIfExists(originImagePath);
        return originImagePath;
    }

    public static string TemplatePathToTemplateImagePath(string templateFilePath)
    {
        var templateImagePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath);
        DirectoryHelper.CreateFileDirectoryIfNotExists(templateImagePath);
        FileHelper.DeleteFileIfExists(templateImagePath);
        return templateImagePath;
    }
}