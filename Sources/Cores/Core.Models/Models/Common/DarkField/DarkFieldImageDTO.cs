using System.Globalization;
using System.IO;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.CIB;
using Core.Models.Models.Common.Pattern;
using HalconDotNet;
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
    private CIBProfileModeEnum _cIBProfileModeEnum;

    [ObservableProperty]
    private bool _isForward;

    [ObservableProperty]
    private string _rawImageFilePath = string.Empty;

    partial void OnRawImageFilePathChanged(string value)
    {
        var result = Path.GetFullPath(value);

        if (value == result) return;

        RawImageFilePath = result;
    }

    #region IEquatable、IFormattable

    public bool Equals(DarkFieldRawScanImageDTO? other) => this == other;

    public override bool Equals(object? obj) => obj is DarkFieldRawScanImageDTO other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(CIBInformation, Size, CIBProfileModeEnum, IsForward, RawImageFilePath);

    public override string ToString() => ToString(null);

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.CurrentCulture;

        return $"CIB: {CIBInformation}, Size: {Size.ToString(format, formatProvider)}, Mode: {CIBProfileModeEnum}, Forward: {IsForward}";
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
                                                   && Equals(left.CIBProfileModeEnum, right.CIBProfileModeEnum)
                                                   && Equals(left.IsForward, right.IsForward)
                                                   && Equals(left.RawImageFilePath, right.RawImageFilePath))
    };

    public static bool operator !=(DarkFieldRawScanImageDTO? left, DarkFieldRawScanImageDTO? right) => !(left == right);

    #endregion Operator

    #region Mapper

    public DarkFieldRawScanImageDTO Clone() => new()
    {
        CIBInformation = CIBInformation.Clone(),
        Size = Size,
        CIBProfileModeEnum = CIBProfileModeEnum,
        IsForward = IsForward,
        RawImageFilePath = RawImageFilePath
    };

    public DarkFieldRawScanImageDTO AdaptIn(M2CImgSysCollectImgDTO obj, CIBProfileModeEnum cibProfileModeEnum)
    {
        Guard.IsNotNull(obj);

        CIBInformation = CIBInformation.Default.Clone().AdaptIn((obj.PMTId, obj.Channel, true));
        Size = new SizeI(obj.ImgWidth, obj.ImgHeight);
        CIBProfileModeEnum = cibProfileModeEnum;
        IsForward = obj.Dir > 0;
        RawImageFilePath = obj.Url;

        return this;
    }

    #endregion Mapper
}

public sealed class DarkFieldImageDTO :
    DarkFieldRawScanImageDTO,
    IEquatable<DarkFieldImageDTO>,
    ICloneable<DarkFieldImageDTO>,
    IAdaptIn<DarkFieldRawScanImageDTO, DarkFieldImageDTO>,
    IDisposable
{
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public required HImage Image { get; init; }

    #region IEquatable、IFormattable

    public bool Equals(DarkFieldImageDTO? other) => this == other;

    #endregion

    #region Mapper

    public new DarkFieldImageDTO Clone() => new()
    {
        CIBInformation = CIBInformation.Clone(),
        Size = Size,
        CIBProfileModeEnum = CIBProfileModeEnum,
        IsForward = IsForward,
        RawImageFilePath = RawImageFilePath,
        Image = Image.Clone()
    };

    public new DarkFieldImageDTO AdaptIn(M2CImgSysCollectImgDTO obj, CIBProfileModeEnum cibProfileModeEnum)
    {
        base.AdaptIn(obj, cibProfileModeEnum);

        return this;
    }

    public DarkFieldImageDTO AdaptIn(DarkFieldRawScanImageDTO obj)
    {
        Guard.IsNotNull(obj);

        CIBInformation = obj.CIBInformation.Clone();
        Size = obj.Size;
        CIBProfileModeEnum = obj.CIBProfileModeEnum;
        IsForward = obj.IsForward;
        RawImageFilePath = obj.RawImageFilePath;

        return this;
    }

    #endregion Mapper

    public void Dispose()
    {
        Image.Dispose();
    }
}