using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using HalconDotNet;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

#if NET
using Semix.GRPC.DTO;
#else
using Semix.WcfTransfer.DTO;

#endif

namespace Core.Models.Models.Common.DarkField;

public sealed partial class DarkFieldImageDto :
    ObservableCacheBase,
    ICloneable<DarkFieldImageDto>,
    IAdaptIn<M2CImgSysCollectImgDTO, DarkFieldImageDto>,
    IAdaptIn<DarkFieldRawScanImageDto, DarkFieldImageDto>,
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
    /// 图片矩阵
    /// </summary>
    public required short[,] Matrix { get; init; }

    /// <summary>
    /// 图片
    /// </summary>
    public required HImage Image { get; init; }

    /// <summary>
    /// 向Y轴投影后的点均值列表
    /// </summary>
    public double[] ProjectionYs
    {
        get
        {
            var convertToDoubleMatrix = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.DenseOfArray(Matrix);
            return [.. convertToDoubleMatrix.RowSums().Divide(convertToDoubleMatrix.ColumnCount)];
        }
    }

    #region Mapper

    public DarkFieldImageDto Clone() => new()
    {
        PmtId = PmtId,
        ChannelId = ChannelId,
        Width = Width,
        Height = Height,
        RawImageFilePath = RawImageFilePath,
        Matrix = MatrixUtils.Clone(Matrix),
        Image = Image.Copy(),
        Id = Id,
        Expiration = Expiration
    };

    public DarkFieldImageDto AdaptIn(M2CImgSysCollectImgDTO obj)
    {
        Guard.IsNotNull(obj);

        PmtId = obj.PMTId;
        ChannelId = obj.Channel;
        Width = obj.ImgWidth;
        Height = obj.ImgHeight;
        RawImageFilePath = obj.Url;

        return this;
    }

    public DarkFieldImageDto AdaptIn(DarkFieldRawScanImageDto obj)
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

public sealed partial class DarkFieldRawScanImageDto :
    ObservableCacheBase,
    ICloneable<DarkFieldRawScanImageDto>,
    IAdaptIn<M2CImgSysCollectImgDTO, DarkFieldRawScanImageDto>
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

    public DarkFieldRawScanImageDto Clone() => new()
    {
        PmtId = PmtId,
        ChannelId = ChannelId,
        Width = Width,
        Height = Height,
        RawImageFilePath = RawImageFilePath,
        Id = Id,
        Expiration = Expiration
    };

    public DarkFieldRawScanImageDto AdaptIn(M2CImgSysCollectImgDTO obj)
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