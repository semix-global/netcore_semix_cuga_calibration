using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using HalconDotNet;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

#if NET
using Semix.GRPC.DTO;
using C2MImgModel = Cuga.Data.DataStruct.DTO.Calibration.CgRawImgModel;
#else
using Semix.WcfTransfer.DTO;

#endif

namespace Core.Models.Models.Common.DarkField;

public sealed partial class DarkFieldImageDto :
    ObservableCacheBase,
    ICloneable<DarkFieldImageDto>,
    IAdaptTo<C2MImgModel>,
    IAdaptIn<C2MImgModel, DarkFieldImageDto>,
    IAdaptIn<M2CImgSysCollectImgDTO, DarkFieldImageDto>,
    IAdaptIn<DarkFieldRawScanImageDto, DarkFieldImageDto>,
    IDisposable
{
    public static DarkFieldImageDto Empty { get; } = new() { Matrix = MatrixUtils.EmptyMatrix<short>(), Image = HalconHelper.EmptyHObject };

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
    /// 16bit raw 图片
    /// </summary>
    [ObservableProperty]
    private byte[] _bytes = [];

    /// <summary>
    /// 图片矩阵
    /// </summary>
    public required short[,] Matrix { get; init; }

    /// <summary>
    /// 图片
    /// </summary>
    public required HObject Image { get; init; }

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

    public DarkFieldImageDto Clone()
    {
        return new DarkFieldImageDto
        {
            PmtId = PmtId,
            ChannelId = ChannelId,
            Width = Width,
            Height = Height,
            Bytes = [.. Bytes],
            Matrix = MatrixUtils.Clone(Matrix),
            Image = HalconHelper.Copy(Image),
            Id = Id,
            Expiration = Expiration
        };
    }

    public C2MImgModel AdaptTo() => new()
    {
        PMTId = PmtId,
        Channel = ChannelId,
        Width = Width,
        Height = Height,
        Img = [.. Bytes]
    };

    public DarkFieldImageDto AdaptIn(C2MImgModel obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        PmtId = obj.PMTId;
        ChannelId = obj.Channel;
        Width = obj.Width;
        Height = obj.Height;
        Bytes = obj.Img is not null ? [.. obj.Img] : [];

        return this;
    }

    public DarkFieldImageDto AdaptIn(M2CImgSysCollectImgDTO obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        PmtId = obj.PMTId;
        ChannelId = obj.Channel;
        Width = obj.ImgWidth;
        Height = obj.ImgHeight;

        return this;
    }

    public DarkFieldImageDto AdaptIn(DarkFieldRawScanImageDto obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        PmtId = obj.PmtId;
        ChannelId = obj.ChannelId;
        Width = obj.Width;
        Height = obj.Height;

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
    IAdaptTo<M2CImgSysCollectImgDTO>,
    IAdaptIn<M2CImgSysCollectImgDTO, DarkFieldRawScanImageDto>
{
    public static DarkFieldImageDto Empty { get; } = new() { Matrix = MatrixUtils.EmptyMatrix<short>(), Image = HalconHelper.EmptyHObject };

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
    /// 图片路径
    /// </summary>
    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private int _direction;

    #region Mapper

    public DarkFieldRawScanImageDto Clone()
    {
        return new DarkFieldRawScanImageDto
        {
            PmtId = PmtId,
            ChannelId = ChannelId,
            Width = Width,
            Height = Height,
            Url = Url,
            Direction = Direction,
            Id = Id,
            Expiration = Expiration
        };
    }

    public M2CImgSysCollectImgDTO AdaptTo() => new()
    {
        PMTId = PmtId,
        Channel = ChannelId,
        ImgWidth = Width,
        ImgHeight = Height,
        Url = Url,
        Dir = Direction
    };

    public DarkFieldRawScanImageDto AdaptIn(M2CImgSysCollectImgDTO obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        PmtId = obj.PMTId;
        ChannelId = obj.Channel;
        Width = obj.ImgWidth;
        Height = obj.ImgHeight;
        Url = obj.Url;
        Direction = obj.Dir;

        return this;
    }

    #endregion Mapper
}