using System.IO;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.CIB;
using HalconDotNet;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Mapper.Interfaces;

#if NET
using Semix.GRPC.DTO;
#else
using Semix.WcfTransfer.DTO;

#endif

namespace Core.Models.Models.Common.DarkField;

public partial class DarkFieldRawScanImageDTO :
    ObservableObject,
    ICloneable<DarkFieldRawScanImageDTO>
{
    [ObservableProperty]
    private int _pMTId;

    [ObservableProperty]
    private int _channelId;

    [ObservableProperty]
    private string _rawImageFilePath = string.Empty;

    [ObservableProperty]
    private int _width;

    [ObservableProperty]
    private int _height;

    [ObservableProperty]
    private CIBProfileModeEnum _cIBProfileModeEnum;

    partial void OnRawImageFilePathChanged(string value)
    {
        var result = Path.GetFullPath(value);

        if (value == result) return;

        RawImageFilePath = result;
    }

    #region Mapper

    public DarkFieldRawScanImageDTO Clone() => new()
    {
        PMTId = PMTId,
        ChannelId = ChannelId,
        Width = Width,
        Height = Height,
        RawImageFilePath = RawImageFilePath,
        CIBProfileModeEnum = CIBProfileModeEnum
    };

    public DarkFieldRawScanImageDTO AdaptIn(M2CImgSysCollectImgDTO obj, CIBProfileModeEnum cibProfileModeEnum)
    {
        Guard.IsNotNull(obj);

        PMTId = obj.PMTId;
        ChannelId = obj.Channel;
        Width = obj.ImgWidth;
        Height = obj.ImgHeight;
        RawImageFilePath = obj.Url;

        CIBProfileModeEnum = cibProfileModeEnum;

        return this;
    }

    #endregion Mapper
}

public sealed class DarkFieldImageDTO :
    DarkFieldRawScanImageDTO,
    ICloneable<DarkFieldImageDTO>,
    IAdaptIn<DarkFieldRawScanImageDTO, DarkFieldImageDTO>,
    IDisposable
{
    public required HImage Image { get; init; }

    #region Mapper

    public new DarkFieldImageDTO Clone() => new()
    {
        PMTId = PMTId,
        ChannelId = ChannelId,
        Width = Width,
        Height = Height,
        RawImageFilePath = RawImageFilePath,
        CIBProfileModeEnum = CIBProfileModeEnum,
        Image = Image.Copy()
    };

    public new DarkFieldImageDTO AdaptIn(M2CImgSysCollectImgDTO obj, CIBProfileModeEnum cibProfileModeEnum)
    {
        Guard.IsNotNull(obj);

        PMTId = obj.PMTId;
        ChannelId = obj.Channel;
        Width = obj.ImgWidth;
        Height = obj.ImgHeight;
        RawImageFilePath = obj.Url;

        CIBProfileModeEnum = cibProfileModeEnum;

        return this;
    }

    public DarkFieldImageDTO AdaptIn(DarkFieldRawScanImageDTO obj)
    {
        Guard.IsNotNull(obj);

        PMTId = obj.PMTId;
        ChannelId = obj.ChannelId;
        Width = obj.Width;
        Height = obj.Height;
        RawImageFilePath = obj.RawImageFilePath;
        CIBProfileModeEnum = obj.CIBProfileModeEnum;

        return this;
    }

    #endregion Mapper

    public void Dispose()
    {
        Image.Dispose();
    }
}