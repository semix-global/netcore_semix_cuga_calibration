using System.Globalization;
using System.IO;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.CIB;
using Core.Models.Models.Common.Pattern;
using Core.Utilities;
using HalconDotNet;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

#if NET
using Semix.GRPC.DTO;
#else
using Semix.WcfTransfer.DTO;
#endif

namespace Core.Models.Models.Common.DarkField;

public partial class DarkFieldRawScanImageDTO :
    ObservableObject,
    IEquatable<DarkFieldRawScanImageDTO>,
    IFormattable,
    ICloneable<DarkFieldRawScanImageDTO>
{
    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private SizeI _size;

    [ObservableProperty]
    private bool _isForward;

    [ObservableProperty]
    private CIBProfileModeEnum _rawImageCIBProfileModeEnum;

    [ObservableProperty]
    private string _rawImageFilePath = string.Empty;

    [ObservableProperty]
    private bool _isKeepRawImageCIBProfileModeEnum = true;

    public CIBProfileModeEnum ImageCIBProfileModeEnum => IsKeepRawImageCIBProfileModeEnum ? RawImageCIBProfileModeEnum : CIBProfileModeEnum.PMTVoltage;

    partial void OnRawImageFilePathChanged(string value)
    {
        var result = Path.GetFullPath(value);

        if (value == result) return;

        RawImageFilePath = result;
    }

    #region IEquatable、IFormattable

    public bool Equals(DarkFieldRawScanImageDTO? other) => this == other;

    public override bool Equals(object? obj) => obj is DarkFieldRawScanImageDTO other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(CIBInformation, Size, RawImageCIBProfileModeEnum, IsForward, RawImageFilePath, IsKeepRawImageCIBProfileModeEnum);

    public override string ToString() => ToString(null);

    public virtual string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.CurrentCulture;

        return $"CIB: {CIBInformation}, Size: {Size.ToString(format, formatProvider)}, Raw Image CIB Profile Mode: {RawImageCIBProfileModeEnum}, Forward: {IsForward}, Image CIB Profile Mode: {ImageCIBProfileModeEnum}";
    }

    #endregion IEquatable、IFormattable

    #region Operator

    public static bool operator ==(DarkFieldRawScanImageDTO? left, DarkFieldRawScanImageDTO? right) => (left, right) switch
    {
        (null, null) => true,
        (null, _) => false,
        (_, null) => false,
        (_, _) => ReferenceEquals(left, right) || (Equals(left.CIBInformation, right.CIBInformation)
                                                   && Equals(left.Size, right.Size)
                                                   && Equals(left.RawImageCIBProfileModeEnum, right.RawImageCIBProfileModeEnum)
                                                   && Equals(left.IsForward, right.IsForward)
                                                   && Equals(left.RawImageFilePath, right.RawImageFilePath)
                                                   && Equals(left.IsKeepRawImageCIBProfileModeEnum, right.IsKeepRawImageCIBProfileModeEnum))
    };

    public static bool operator !=(DarkFieldRawScanImageDTO? left, DarkFieldRawScanImageDTO? right) => !(left == right);

    #endregion Operator

    #region Mapper

    public DarkFieldRawScanImageDTO Clone() => new()
    {
        CIBInformation = CIBInformation.Clone(),
        Size = Size,
        IsForward = IsForward,
        RawImageCIBProfileModeEnum = RawImageCIBProfileModeEnum,
        RawImageFilePath = RawImageFilePath,
        IsKeepRawImageCIBProfileModeEnum = IsKeepRawImageCIBProfileModeEnum
    };

    public DarkFieldRawScanImageDTO AdaptIn(M2CImgSysCollectImgDTO obj, bool isForward, CIBProfileModeEnum rawCIBProfileModeEnum, bool isKeepRawImageCIBProfileModeEnum)
    {
        Guard.IsNotNull(obj);

        CIBInformation = CIBInformation.Default.Clone().AdaptIn((obj.PMTId, obj.Channel, true));
        Size = new SizeI(obj.ImgWidth, obj.ImgHeight);
        IsForward = isForward;
        RawImageCIBProfileModeEnum = rawCIBProfileModeEnum;
        RawImageFilePath = obj.Url;
        IsKeepRawImageCIBProfileModeEnum = isKeepRawImageCIBProfileModeEnum;

        return this;
    }

    #endregion Mapper

    public HImage GetImage()
    {
        var rawBytes = File.ReadAllBytes(RawImageFilePath);

        using var image = RawImageFactory.CreateImage(rawBytes);

        return IsKeepRawImageCIBProfileModeEnum
            ? image.CopyImage()
            : RawImageCIBProfileModeEnum == CIBProfileModeEnum.PMTLog
                ? image.RAW12BitsPerPixelLogToLinear()
                : image.Clone();
    }

    public virtual object ToHtmlAnonymous() => new
    {
        CIBInformation,
        Size,
        RawImageCIBProfileModeEnum,
        IsForward,
        RawImageFilePath,
        IsKeepRawImageCIBProfileModeEnum,
        ImageCIBProfileModeEnum
    };
}

public sealed class DarkFieldImageDTO :
    DarkFieldRawScanImageDTO,
    IEquatable<DarkFieldImageDTO>,
    ICloneable<DarkFieldImageDTO>,
    IAdaptIn<DarkFieldRawScanImageDTO, DarkFieldImageDTO>,
    IDisposable
{
#pragma warning disable IDE0079
#pragma warning disable IDISP008

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public HImage Image { get; private set; } = HalconFactory.EmptyHImage;

#pragma warning restore IDISP008
#pragma warning restore IDE0079

    #region IEquatable

    public bool Equals(DarkFieldImageDTO? other) => this == other;

    #endregion IEquatable

    #region Mapper

    public new DarkFieldImageDTO Clone()
    {
        var darkFieldImage = (DarkFieldImageDTO)base.Clone();

        darkFieldImage.Image = Image.Clone();

        return darkFieldImage;
    }

    public new DarkFieldImageDTO AdaptIn(M2CImgSysCollectImgDTO obj, bool isForward, CIBProfileModeEnum rawCIBProfileModeEnum, bool isKeepRawImageCIBProfileModeEnum)
    {
        base.AdaptIn(obj, isForward, rawCIBProfileModeEnum, isKeepRawImageCIBProfileModeEnum);

        Initialize();

        return this;
    }

    public DarkFieldImageDTO AdaptIn(DarkFieldRawScanImageDTO obj)
    {
        CIBInformation = obj.CIBInformation.Clone();
        Size = obj.Size;
        IsForward = obj.IsForward;
        RawImageCIBProfileModeEnum = obj.RawImageCIBProfileModeEnum;
        RawImageFilePath = obj.RawImageFilePath;
        IsKeepRawImageCIBProfileModeEnum = obj.IsKeepRawImageCIBProfileModeEnum;

        Initialize();

        return this;
    }

    private void Initialize()
    {
        using var _ = Image;

        Image = GetImage();
    }

    #endregion Mapper

    public void Dispose()
    {
        Image.Dispose();
    }
}