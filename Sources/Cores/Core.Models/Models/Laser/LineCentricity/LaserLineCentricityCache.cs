using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace Core.Models.Models.Laser.LineCentricity;

public sealed partial class LaserLineCentricityCache : CalibrationCacheBase
{
    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum = CalibrationConstantsHelper.MainOpticsIlluminationModeEnum;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.GridConrner_100um;

    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private double _chuckRadius = 150000;

    [ObservableProperty]
    private double _pmtInterval = 320; // Pmt相机采集间隔320um

    [ObservableProperty]
    private double _p5Angle;

    [ObservableProperty]
    private bool _isDarkFieldAlignment;

    [JsonConverter(typeof(LaserLineCentricityCacheItemConverter))]
    public ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), LaserLineCentricityCacheItem>> Items { get; init; } = [];

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public LaserLineCentricityCacheItem Item => Items.GetOrAdd((OpticsIlluminationModeEnum, ProductivityInformation), new LaserLineCentricityCacheItem());

    private sealed class LaserLineCentricityCacheItemConverter : JsonConverter<ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), LaserLineCentricityCacheItem>>>
    {
        public override void WriteJson(JsonWriter writer, ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), LaserLineCentricityCacheItem>>? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();

                return;
            }

            writer.WriteStartArray();

            foreach (var kvp in value)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(nameof(kvp.Key));
                writer.WriteStartObject();
                writer.WritePropertyName(nameof(kvp.Key.Item1));
                writer.WriteValue((int)kvp.Key.Item1);
                writer.WritePropertyName(nameof(kvp.Key.Item2));
                serializer.Serialize(writer, kvp.Key.Item2);
                writer.WriteEndObject();

                writer.WritePropertyName(nameof(kvp.Value));
                serializer.Serialize(writer, kvp.Value);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        public override ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), LaserLineCentricityCacheItem>> ReadJson(JsonReader reader, Type objectType, ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), LaserLineCentricityCacheItem>>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return [];

            var jsonArray = JArray.Load(reader);
            var result = new ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), LaserLineCentricityCacheItem>>();

            foreach (var item in jsonArray)
            {
                var keyToken = item[nameof(KeyValuePair<,>.Key)];
                var valueToken = item[nameof(KeyValuePair<,>.Value)];

                if (keyToken is null || valueToken is null) continue;

                var opticsMode = (OpticsIlluminationModeEnum)(keyToken[nameof(ValueTuple<,>.Item1)]?.Value<int>() ?? keyToken[nameof(OpticsIlluminationModeEnum)]?.Value<int>() ?? (int)OpticsIlluminationModeEnum.OI);
                var productivityInfo = (keyToken[nameof(ValueTuple<,>.Item2)] ?? keyToken[nameof(ProductivityInformation)])?.ToObject<ProductivityInformation>(serializer) ?? ProductivityInformation.Default;

                var value = valueToken.ToObject<LaserLineCentricityCacheItem>(serializer) ?? new LaserLineCentricityCacheItem();

                result.Add(new KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), LaserLineCentricityCacheItem>((opticsMode, productivityInfo), value));
            }

            return result;
        }
    }
}

public sealed partial class LaserLineCentricityCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    /// <summary>
    /// 选定特征的明场坐标
    /// </summary>
    [ObservableProperty]
    private Point _findPosition;

    /// <summary>
    /// 明场选定特征对应的stage机械坐标
    /// </summary>
    [ObservableProperty]
    private Point _findBrightMachinePosition;

    [ObservableProperty]
    private int _xWidthPixel = 800;

    [ObservableProperty]
    private string _brightTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _brightTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _templateFilePath = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath = string.Empty;

    [ObservableProperty]
    private Point _threshold;
}