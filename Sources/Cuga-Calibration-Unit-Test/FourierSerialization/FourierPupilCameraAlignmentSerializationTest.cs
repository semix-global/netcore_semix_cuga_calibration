using AwesomeAssertions;
using AwesomeAssertions.Execution;
using CommunityToolkit.Diagnostics;
using Core.Models.Models.Fourier.PupilCameraAlignment;
using Net.Utilities.Models.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.Cache.Providers.Serializations;
using Local.SQL.Cache.Providers.Services.Interfaces;

namespace CugaCalibrationUnitTest.FourierSerialization;

public sealed class FourierPupilCameraAlignmentSerializationTest
{
    private static readonly string[] NonPersistentNames =
    [
        "Document",
        "_originalBitmapImageDrawable",
        "_roiBitmapImageDrawable",
        "_bitmapImageROIDrawable"
    ];

    private static readonly string[] IgnoreProperties =
    [
        nameof(ICacheItem.Id),
        nameof(ICacheItem.Expiration),
        nameof(ICacheItem.CreatedUserId),
        nameof(ICacheItem.CreatedUserName),
        nameof(ICacheItem.CreatedTime),
        nameof(ICacheItem.ModifiedUserId),
        nameof(ICacheItem.ModifiedUserName),
        nameof(ICacheItem.ModifiedTime),
        nameof(ICacheItem.IsDeleted),
        nameof(ObservableValidator.HasErrors)
    ];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProvidedJson_ShouldMatchEveryField_AndSerializeBackIdentically(bool useCacheSettings)
    {
        var settings = useCacheSettings ? IgnoreCacheItemPropertiesContractResolver.Settings : new JsonSerializerSettings();
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "FourierSerialization", "FourierPupilCameraAlignment.json"));
        var expected = JObject.Parse(json);

#pragma warning disable IDE0079
#pragma warning disable IDISP004

        using var actual = Guard.IsNotNullAndReturn(JsonConvert.DeserializeObject<FourierPupilCameraAlignmentDTO>(json, settings));

#pragma warning restore IDISP004
#pragma warning restore IDE0079

        AssertMatchesJson();

        var saved = JObject.Parse(JsonConvert.SerializeObject(actual, settings));
        saved.Descendants().OfType<JProperty>().Select(t => t.Name).Should().NotContain(NonPersistentNames);

        if (useCacheSettings == false) RemoveMetadata(saved);

        saved.ToString(Formatting.Indented).Should().Be(expected.ToString(Formatting.Indented));

        return;

        void AssertMatchesJson()
        {
            using var scope = new AssertionScope(expected.Path);

            actual.IsCalibrated.Should().Be(expected.Value<bool>(nameof(actual.IsCalibrated)));
            actual.IsVerified.Should().Be(expected.Value<bool>(nameof(actual.IsVerified)));
            actual.IsRequiredSelfCheck.Should().Be(expected.Value<bool>(nameof(actual.IsRequiredSelfCheck)));
            actual.IsOk.Should().Be(expected.Value<bool>(nameof(actual.IsOk)));

            AssertChannelMatchesJson(actual.Channel1Item, Guard.IsNotNullAndAssignableToTypeAndReturn<JObject>(expected[nameof(actual.Channel1Item)]));
            AssertChannelMatchesJson(actual.Channel2Item, Guard.IsNotNullAndAssignableToTypeAndReturn<JObject>(expected[nameof(actual.Channel2Item)]));
            AssertChannelMatchesJson(actual.Channel3Item, Guard.IsNotNullAndAssignableToTypeAndReturn<JObject>(expected[nameof(actual.Channel3Item)]));
        }

        static void AssertChannelMatchesJson(FourierPupilCameraAlignmentDTOItem actual, JObject expected)
        {
            using var scope = new AssertionScope(expected.Path);

            actual.ChannelId.Should().Be(expected.Value<int>(nameof(actual.ChannelId)));
            actual.ChannelImageFilePath.Should().Be(expected.Value<string>(nameof(actual.ChannelImageFilePath)));
            actual.ROIChannelImageFilePath.Should().Be(expected.Value<string>(nameof(actual.ROIChannelImageFilePath)));

            AssertRectMatchesJson(actual.ImageROI, Guard.IsNotNullAndAssignableToTypeAndReturn<JToken>(expected[nameof(actual.ImageROI)]));
        }

        static void AssertRectMatchesJson(Rect actual, JToken expected)
        {
            using var scope = new AssertionScope(expected.Path);

            actual.X.Should().Be(expected.Value<double>(nameof(actual.X)));
            actual.Y.Should().Be(expected.Value<double>(nameof(actual.Y)));
            actual.Width.Should().Be(expected.Value<double>(nameof(actual.Width)));
            actual.Height.Should().Be(expected.Value<double>(nameof(actual.Height)));
        }

        static void RemoveMetadata(JToken jToken)
        {
            switch (jToken)
            {
                case JArray array:
                    foreach (var item in array) RemoveMetadata(item);

                    break;

                case JObject obj:
                    foreach (var ignoreProperty in IgnoreProperties) obj.Remove(ignoreProperty);

                    foreach (var property in obj.Properties()) RemoveMetadata(property.Value);

                    break;
            }
        }
    }

    [Theory]
    [InlineData(11, false)]
    [InlineData(22, true)]
    [InlineData(33, false)]
    [InlineData(44, true)]
    [InlineData(55, false)]
    public void RandomDto_ShouldMatchEveryField_AfterJsonRoundTrip(int seed, bool useCacheSettings)
    {
        var settings = useCacheSettings ? IgnoreCacheItemPropertiesContractResolver.Settings : new JsonSerializerSettings();

        using var expected = CreateRandomDto(new Random(seed));

#pragma warning disable IDE0079
#pragma warning disable IDISP004

        using var actual = Guard.IsNotNullAndReturn(JsonConvert.DeserializeObject<FourierPupilCameraAlignmentDTO>(JsonConvert.SerializeObject(expected, settings), settings));

#pragma warning restore IDISP004
#pragma warning restore IDE0079

        AssertEquals();

        return;

        static FourierPupilCameraAlignmentDTO CreateRandomDto(Random random)
        {
            var dto = new FourierPupilCameraAlignmentDTO
            {
                IsCalibrated = random.Next(2) == 1,
                IsVerified = random.Next(2) == 1,
                IsRequiredSelfCheck = random.Next(2) == 1,
                Id = ((long)random.Next() << 31) ^ random.Next(),
                Expiration = ((long)random.Next() << 31) ^ random.Next()
            };

            foreach (var channel in new[] { dto.Channel1Item, dto.Channel2Item, dto.Channel3Item })
            {
                channel.ChannelImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\Channel_{random.Next(0, 99)}.jpg";
                channel.ROIChannelImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\ROI_{random.Next(0, 99)}.jpg";
                channel.ImageROI = random.Next(6) switch
                {
                    0 => Rect.Empty,
                    1 => new Rect(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), 0d, 0d),
                    _ => new Rect(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Abs(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3)), Math.Abs(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3)))
                };
            }

            return dto;
        }

        void AssertEquals()
        {
            using var scope = new AssertionScope("DTO");

            actual.IsCalibrated.Should().Be(expected.IsCalibrated);
            actual.IsVerified.Should().Be(expected.IsVerified);
            actual.IsRequiredSelfCheck.Should().Be(expected.IsRequiredSelfCheck);
            actual.IsOk.Should().Be(expected.IsOk);
            if (useCacheSettings == false)
            {
                actual.Id.Should().Be(expected.Id);
                actual.Expiration.Should().Be(expected.Expiration);
            }

            AssertChannelEquals(actual.Channel1Item, expected.Channel1Item);
            AssertChannelEquals(actual.Channel2Item, expected.Channel2Item);
            AssertChannelEquals(actual.Channel3Item, expected.Channel3Item);
        }

        static void AssertChannelEquals(FourierPupilCameraAlignmentDTOItem actual, FourierPupilCameraAlignmentDTOItem expected)
        {
            actual.ChannelId.Should().Be(expected.ChannelId);
            actual.ChannelImageFilePath.Should().Be(expected.ChannelImageFilePath);
            actual.ROIChannelImageFilePath.Should().Be(expected.ROIChannelImageFilePath);
            actual.ImageROI.Should().Be(expected.ImageROI);
        }
    }
}