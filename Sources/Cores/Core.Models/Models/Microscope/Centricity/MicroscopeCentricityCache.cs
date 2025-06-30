using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Microscope.Centricity;

public sealed partial class MicroscopeCentricityCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private Point _templateFindPosition150X;

    [ObservableProperty]
    private Point _templateFindPosition100X;

    [ObservableProperty]
    private Point _templateFindPosition50X;

    [ObservableProperty]
    private Point _templateFindPosition10X;

    [ObservableProperty]
    private Point _templateFindPosition5X;

    [ObservableProperty]
    private string _templateFilePath150X = string.Empty;

    [ObservableProperty]
    private string _templateFilePath100X = string.Empty;

    [ObservableProperty]
    private string _templateFilePath50X = string.Empty;

    [ObservableProperty]
    private string _templateFilePath10X = string.Empty;

    [ObservableProperty]
    private string _templateFilePath5X = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath150X = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath100X = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath50X = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath10X = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath5X = string.Empty;

    [ObservableProperty]
    private Point _verifyResultPosition;

    [ObservableProperty]
    private Point _verifyResultError;

    [ObservableProperty]
    private Point _threshold;

    [ObservableProperty]
    private double _concentricThreshold;

    public Point GetTemplateFindPosition() => MicroscopeMagnificationEnum switch
    {
        MicroscopeMagnificationEnum.Magnification5X => TemplateFindPosition5X,
        MicroscopeMagnificationEnum.Magnification10X => TemplateFindPosition10X,
        MicroscopeMagnificationEnum.Magnification50X => TemplateFindPosition50X,
        MicroscopeMagnificationEnum.Magnification100X => TemplateFindPosition100X,
        MicroscopeMagnificationEnum.Magnification150X => TemplateFindPosition150X,
        _ => throw new ArgumentOutOfRangeException()
    };

    public string GetTemplateFilePath() => MicroscopeMagnificationEnum switch
    {
        MicroscopeMagnificationEnum.Magnification5X => TemplateFilePath5X,
        MicroscopeMagnificationEnum.Magnification10X => TemplateFilePath10X,
        MicroscopeMagnificationEnum.Magnification50X => TemplateFilePath50X,
        MicroscopeMagnificationEnum.Magnification100X => TemplateFilePath100X,
        MicroscopeMagnificationEnum.Magnification150X => TemplateFilePath150X,
        _ => throw new ArgumentOutOfRangeException()
    };

    public string GetTemplateImageFilePath() => MicroscopeMagnificationEnum switch
    {
        MicroscopeMagnificationEnum.Magnification5X => TemplateImageFilePath5X,
        MicroscopeMagnificationEnum.Magnification10X => TemplateImageFilePath10X,
        MicroscopeMagnificationEnum.Magnification50X => TemplateImageFilePath50X,
        MicroscopeMagnificationEnum.Magnification100X => TemplateImageFilePath100X,
        MicroscopeMagnificationEnum.Magnification150X => TemplateImageFilePath150X,
        _ => throw new ArgumentOutOfRangeException()
    };
}