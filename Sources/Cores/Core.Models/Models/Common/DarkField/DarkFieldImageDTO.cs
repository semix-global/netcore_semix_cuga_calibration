using System.Globalization;
using System.IO;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.CIB;
using Core.Models.Models.Common.Pattern;
using HalconDotNet;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
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

    protected virtual DarkFieldRawScanImageDTO CreateInstance() => new();

    #region IEquatable、IFormattable

    public bool Equals(DarkFieldRawScanImageDTO? other) => this == other;

    public override bool Equals(object? obj) => obj is DarkFieldRawScanImageDTO other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(CIBInformation, Size, RawImageCIBProfileModeEnum, IsForward, RawImageFilePath, IsKeepRawImageCIBProfileModeEnum);

    public override string ToString() => ToString(null);

    public virtual string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.CurrentCulture;

        return $"CIB: {CIBInformation}, Size: {Size.ToString(format, formatProvider)}, Raw Mode: {RawImageCIBProfileModeEnum}, Image Direction: {(IsForward ? "Forward" : "Reverse")}, Image Mode: {ImageCIBProfileModeEnum}";
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

    public DarkFieldRawScanImageDTO Clone()
    {
        var instance = CreateInstance();
        CIBInformation = CIBInformation.Clone();
        instance.Size = Size;
        instance.IsForward = IsForward;
        instance.RawImageCIBProfileModeEnum = RawImageCIBProfileModeEnum;
        instance.RawImageFilePath = RawImageFilePath;
        instance.IsKeepRawImageCIBProfileModeEnum = IsKeepRawImageCIBProfileModeEnum;
        return instance;
    }

    public DarkFieldRawScanImageDTO AdaptIn(M2CImgSysCollectImgDTO obj, bool isForward, CIBProfileModeEnum rawCIBProfileModeEnum, bool isKeepRawImageCIBProfileModeEnum)
    {
        Guard.IsNotNull(obj);
        var (size, _, _) = RAWImageFactory.GetSize(obj.Url);

        CIBInformation = CIBInformation.Default.Clone().AdaptIn((obj.PMTId, obj.Channel, true));
        Size = size;
        IsForward = isForward;
        RawImageCIBProfileModeEnum = rawCIBProfileModeEnum;
        RawImageFilePath = obj.Url;
        IsKeepRawImageCIBProfileModeEnum = isKeepRawImageCIBProfileModeEnum;

        return this;
    }

    #endregion Mapper

    public BitmapImage GetImage()
    {
        var rawBytes = File.ReadAllBytes(RawImageFilePath);

        using var hImage = IsKeepRawImageCIBProfileModeEnum
            ? RAWImageFactory.CreateImage(rawBytes, false)
            : RawImageCIBProfileModeEnum == CIBProfileModeEnum.PMTLog
                ? RAWImageFactory.CreateImage(rawBytes, true)
                : RAWImageFactory.CreateImage(rawBytes, false);

        return hImage.ToBitmapImage(RawImageCIBProfileModeEnum == CIBProfileModeEnum.PMTVoltage ? 16 : 12);
    }

    private static HImage HObjectToHImage(HObject hObject)
    {
        var image = new HImage();
        HOperatorSet.GetImagePointer1(hObject, out var pointer, out var type, out var width, out var height);
        using var _0 = pointer;
        using var _1 = type;
        using var _2 = width;
        using var _3 = height;
        image.GenImage1(type, width, height, pointer);

        return image;
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
#pragma warning disable IDISP005
#pragma warning disable IDISP008

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public BitmapImage Image { get; private set; } = Utilities.BitmapImageExtensions.Empty;

    protected override DarkFieldRawScanImageDTO CreateInstance() => new DarkFieldImageDTO();

#pragma warning restore IDISP008
#pragma warning restore IDISP005

    #region IEquatable

    public bool Equals(DarkFieldImageDTO? other) => this == other;

    #endregion IEquatable

    #region Mapper

    public new DarkFieldImageDTO Clone()
    {
        var darkFieldImage = (DarkFieldImageDTO)base.Clone();

        darkFieldImage.Image.Dispose();
        darkFieldImage.Image = Image.Copy();

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
        Image.Dispose();
        Image = GetImage();

        Guard.IsTrue(new SizeI(Image.ImageInfo.Width, Image.ImageInfo.Height) == Size);
    }

    #endregion Mapper

    public void Dispose()
    {
        Image.Dispose();
    }
}