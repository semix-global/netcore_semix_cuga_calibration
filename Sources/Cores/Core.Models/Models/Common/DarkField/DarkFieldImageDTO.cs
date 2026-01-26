using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using HalconDotNet;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

#if NET
using Semix.GRPC.DTO;
#else
using Semix.WcfTransfer.DTO;

#endif

namespace Core.Models.Models.Common.DarkField;

public sealed partial class DarkFieldImageDTO :
    ObservableCacheBase,
    ICloneable<DarkFieldImageDTO>,
    IAdaptIn<M2CImgSysCollectImgDTO, DarkFieldImageDTO>,
    IAdaptIn<DarkFieldRawScanImageDTO, DarkFieldImageDTO>,
    IDisposable
{
    /// <summary>
    /// PMT Id
    /// </summary>
    [ObservableProperty]
    private int _pmtId;

    /// <summary>
    /// 通道 Id
    /// </summary>
    [ObservableProperty]
    private int _channelId;

    /// <summary>
    /// 图片宽度
    /// </summary>
    [ObservableProperty]
    private int _width;

    /// <summary>
    /// 图片高度
    /// </summary>
    [ObservableProperty]
    private int _height;

    /// <summary>
    /// RAW文件路径
    /// </summary>
    [ObservableProperty]
    private string _rawImageFilePath = string.Empty;

    /// <summary>
    /// 图片
    /// </summary>
    public required HImage Image { get; init; }

    #region Mapper

    public DarkFieldImageDTO Clone() => new()
    {
        PmtId = PmtId,
        ChannelId = ChannelId,
        Width = Width,
        Height = Height,
        RawImageFilePath = RawImageFilePath,
        Image = Image.Copy(),
        Id = Id,
        Expiration = Expiration
    };

    public DarkFieldImageDTO AdaptIn(M2CImgSysCollectImgDTO obj)
    {
        Guard.IsNotNull(obj);

        PmtId = obj.PMTId;
        ChannelId = obj.Channel;
        Width = obj.ImgWidth;
        Height = obj.ImgHeight;
        RawImageFilePath = obj.Url;

        return this;
    }

    public DarkFieldImageDTO AdaptIn(DarkFieldRawScanImageDTO obj)
    {
        Guard.IsNotNull(obj);

        PmtId = obj.PmtId;
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

public sealed partial class DarkFieldRawScanImageDTO :
    ObservableCacheBase,
    ICloneable<DarkFieldRawScanImageDTO>,
    IAdaptIn<M2CImgSysCollectImgDTO, DarkFieldRawScanImageDTO>
{
    /// <summary>
    /// PMT Id
    /// </summary>
    [ObservableProperty]
    private int _pmtId;

    /// <summary>
    /// 通道 Id
    /// </summary>
    [ObservableProperty]
    private int _channelId;

    /// <summary>
    /// RAW文件路径
    /// </summary>
    [ObservableProperty]
    private string _rawImageFilePath = string.Empty;

    /// <summary>
    /// 图片宽度
    /// </summary>
    [ObservableProperty]
    private int _width;

    /// <summary>
    /// 图片高度
    /// </summary>
    [ObservableProperty]
    private int _height;

    #region Mapper

    public DarkFieldRawScanImageDTO Clone() => new()
    {
        PmtId = PmtId,
        ChannelId = ChannelId,
        Width = Width,
        Height = Height,
        RawImageFilePath = RawImageFilePath,
        Id = Id,
        Expiration = Expiration
    };

    public DarkFieldRawScanImageDTO AdaptIn(M2CImgSysCollectImgDTO obj)
    {
        Guard.IsNotNull(obj);

        PmtId = obj.PMTId;
        ChannelId = obj.Channel;
        Width = obj.ImgWidth;
        Height = obj.ImgHeight;
        RawImageFilePath = obj.Url;

        return this;
    }

    #endregion Mapper
}
