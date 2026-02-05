using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
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
    ICloneable<DarkFieldRawScanImageDTO>,
    IAdaptIn<M2CImgSysCollectImgDTO, DarkFieldRawScanImageDTO>
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

    #region Mapper

    public DarkFieldRawScanImageDTO Clone() => new()
    {
        PMTId = PMTId,
        ChannelId = ChannelId,
        Width = Width,
        Height = Height,
        RawImageFilePath = RawImageFilePath
    };

    public DarkFieldRawScanImageDTO AdaptIn(M2CImgSysCollectImgDTO obj)
    {
        Guard.IsNotNull(obj);

        PMTId = obj.PMTId;
        ChannelId = obj.Channel;
        Width = obj.ImgWidth;
        Height = obj.ImgHeight;
        RawImageFilePath = obj.Url;

        return this;
    }

    #endregion Mapper
}

public sealed class DarkFieldImageDTO :
    DarkFieldRawScanImageDTO,
    ICloneable<DarkFieldImageDTO>,
    IAdaptIn<M2CImgSysCollectImgDTO, DarkFieldImageDTO>,
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
        Image = Image.Copy()
    };

    public new DarkFieldImageDTO AdaptIn(M2CImgSysCollectImgDTO obj)
    {
        Guard.IsNotNull(obj);

        PMTId = obj.PMTId;
        ChannelId = obj.Channel;
        Width = obj.ImgWidth;
        Height = obj.ImgHeight;
        RawImageFilePath = obj.Url;

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

        return this;
    }

    #endregion Mapper

    public void Dispose()
    {
        Image.Dispose();
    }
}