using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace Core.Models.Models.Chuck.AlignmentDegreeOffset;

public sealed partial class ChuckAlignmentDegreeOffsetCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _lowMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = MicroscopeLensInformation.Default;

    /// <summary>
    /// 晶圆类型
    /// </summary>
    [ObservableProperty]
    private AlgorithmWaferTypeEnum _algorithmWaferTypeEnum = AlgorithmWaferTypeEnum.D300;

    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum = CalibrationConstantsHelper.MainOpticsIlluminationModeEnum;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [JsonConverter(typeof(ChuckAlignmentDegreeOffsetCacheItemConverter))]
    public ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), ChuckAlignmentDegreeOffsetCacheItem>> Items { get; init; } = [];

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public ChuckAlignmentDegreeOffsetCacheItem Item => Items.GetOrAdd((OpticsIlluminationModeEnum, ProductivityInformation), new ChuckAlignmentDegreeOffsetCacheItem());

    [ObservableProperty]
    private double _nccTypeTemplateMatchScoreThreshold = 0.8;

    [ObservableProperty]
    private double _teachingThreshold;

    [ObservableProperty]
    private double _verifyThreshold;

    private sealed class ChuckAlignmentDegreeOffsetCacheItemConverter : JsonConverter<ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), ChuckAlignmentDegreeOffsetCacheItem>>>
    {
        public override void WriteJson(JsonWriter writer, ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), ChuckAlignmentDegreeOffsetCacheItem>>? value, JsonSerializer serializer)
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

        public override ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), ChuckAlignmentDegreeOffsetCacheItem>> ReadJson(JsonReader reader, Type objectType, ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), ChuckAlignmentDegreeOffsetCacheItem>>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return [];

            var jsonArray = JArray.Load(reader);
            var result = new ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), ChuckAlignmentDegreeOffsetCacheItem>>();

            foreach (var item in jsonArray)
            {
                var keyToken = item[nameof(KeyValuePair<,>.Key)];
                var valueToken = item[nameof(KeyValuePair<,>.Value)];

                if (keyToken is null || valueToken is null) continue;

                var opticsMode = (OpticsIlluminationModeEnum)(keyToken[nameof(ValueTuple<,>.Item1)]?.Value<int>() ?? keyToken[nameof(OpticsIlluminationModeEnum)]?.Value<int>() ?? (int)OpticsIlluminationModeEnum.OI);
                var productivityInfo = (keyToken[nameof(ValueTuple<,>.Item2)] ?? keyToken[nameof(ProductivityInformation)])?.ToObject<ProductivityInformation>(serializer) ?? ProductivityInformation.Default;

                var value = valueToken.ToObject<ChuckAlignmentDegreeOffsetCacheItem>(serializer) ?? new ChuckAlignmentDegreeOffsetCacheItem();

                result.Add(new KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), ChuckAlignmentDegreeOffsetCacheItem>((opticsMode, productivityInfo), value));
            }

            return result;
        }
    }
}

public sealed partial class ChuckAlignmentDegreeOffsetCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private int _xWidthPixel = 800;
}