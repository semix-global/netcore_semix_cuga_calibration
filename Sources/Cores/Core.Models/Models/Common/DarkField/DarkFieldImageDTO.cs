using System.Globalization;
using System.IO;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.CIB;
using Core.Models.Models.Common.Pattern;
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
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial SizeI Size { get; set; }

    [ObservableProperty]
    public partial bool IsForward { get; set; }

    [ObservableProperty]
    public partial CIBProfileModeEnum RawImageCIBProfileModeEnum { get; set; }

    [ObservableProperty]
    public partial string RawImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsKeepRawImageCIBProfileModeEnum { get; set; } = true;

    public bool IsToLiner => IsKeepRawImageCIBProfileModeEnum == false && RawImageCIBProfileModeEnum == CIBProfileModeEnum.PMTLog;

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

    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => HashCode.Combine(CIBInformation, Size, RawImageCIBProfileModeEnum, IsForward, RawImageFilePath, IsKeepRawImageCIBProfileModeEnum);
    // ReSharper restore NonReadonlyMemberInGetHashCode

    public override string ToString() => ToString(null);

    public virtual string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.CurrentCulture;

        return $"CIB: {CIBInformation}, Size: {Size.ToString(format, formatProvider)}, Raw Mode: {RawImageCIBProfileModeEnum}, Image Direction: {(IsForward ? "Forward" : "Reverse")}, Is To Liner: {IsToLiner}";
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

        instance.CIBInformation = CIBInformation.Clone();
        instance.Size = Size;
        instance.IsForward = IsForward;
        instance.RawImageCIBProfileModeEnum = RawImageCIBProfileModeEnum;
        instance.RawImageFilePath = RawImageFilePath;
        instance.IsKeepRawImageCIBProfileModeEnum = IsKeepRawImageCIBProfileModeEnum;

        return instance;
    }

    public DarkFieldRawScanImageDTO AdaptIn(M2CImgSysCollectImgDTO obj, CIBInformation cibInformation, bool isForward, CIBProfileModeEnum rawCIBProfileModeEnum, bool isKeepRawImageCIBProfileModeEnum)
    {
        Guard.IsNotNull(obj);
        var (size, _, _) = RAWImageFactory.GetSize(obj.Url);

        CIBInformation = cibInformation.Clone();
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
        using var hImage = RAWImageFactory.CreateImage(RawImageFilePath, IsToLiner);

        return hImage.ToBitmapImage(IsToLiner ? 16 : 12);
    }

    public object ToHtmlAnonymous() => new
    {
        CIBInformation,
        Size,
        RawImageCIBProfileModeEnum,
        IsForward,
        RawImageFilePath,
        IsKeepRawImageCIBProfileModeEnum,
        IsToLiner
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

    public new DarkFieldImageDTO AdaptIn(M2CImgSysCollectImgDTO obj, CIBInformation cibInformation, bool isForward, CIBProfileModeEnum rawCIBProfileModeEnum, bool isKeepRawImageCIBProfileModeEnum)
    {
        base.AdaptIn(obj, cibInformation, isForward, rawCIBProfileModeEnum, isKeepRawImageCIBProfileModeEnum);

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
    }

    #endregion Mapper

    public void Dispose()
    {
        Image.Dispose();
    }
}