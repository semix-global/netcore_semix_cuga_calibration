using AwesomeAssertions;
using AwesomeAssertions.Execution;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Fourier.SideChannelSpecularBlocker;
using Local.SQL.Cache.Providers.Serializations;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Models.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace CugaCalibrationUnitTest.FourierSerialization;

/// <summary>
/// ProductivityInformation 这个不判断, 否则需要注入IOC容器
/// </summary>
public sealed class FourierSideChannelSpecularBlockerSerializationTest
{
    private static readonly string[] NonPersistentNames =
    [
        "Document",
        "CIBDocument",
        "BitmapImageROIDrawable",
        "_step0FourierBitmapImageDrawable",
        "_step1FourierBitmapImageDrawable",
        "_step0CIBBitmapImageDrawable",
        "_step1CIBBitmapImageDrawable",
        "_step1FourierROIDrawables"
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
        nameof(ObservableValidator.HasErrors)
    ];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProvidedJson_ShouldMatchEveryField_AndSerializeBackIdentically(bool useCacheSettings)
    {
        var settings = useCacheSettings ? IgnoreCacheItemPropertiesContractResolver.Settings : new JsonSerializerSettings();
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "FourierSerialization", "FourierSideChannelSpecularBlocker.json"));
        var expected = JObject.Parse(json);

#pragma warning disable IDE0079
#pragma warning disable IDISP004

        using var actual = Deserialize(json, settings);

#pragma warning restore IDISP004
#pragma warning restore IDE0079

        AssertMatchesJson();

        var saved = JObject.Parse(JsonConvert.SerializeObject(actual, settings));
        saved.Descendants().OfType<JProperty>().Select(t => t.Name).Should().NotContain(NonPersistentNames);

        if (useCacheSettings == false)
        {
            RemoveMetadata(saved);
            saved.Remove(nameof(ICacheItem.IsDeleted)); // Rod 不能删除
        }

        saved.ToString(Formatting.Indented).Should().Be(expected.ToString(Formatting.Indented));

        return;

        void AssertMatchesJson()
        {
            using var scope = new AssertionScope(expected.Path);

            ObjectHelper.GetFieldValue(actual, "_rodTotalCount").Should().Be(expected.Value<int>("_rodTotalCount"));
            actual.IsCalibrated.Should().Be(expected.Value<bool>(nameof(actual.IsCalibrated)));
            actual.IsVerified.Should().Be(expected.Value<bool>(nameof(actual.IsVerified)));
            actual.IsRequiredSelfCheck.Should().Be(expected.Value<bool>(nameof(actual.IsRequiredSelfCheck)));
            actual.IsOk.Should().Be(expected.Value<bool>(nameof(actual.IsOk)));

            AssertChannelMatchesJson(actual.Channel1Item, Guard.IsNotNullAndAssignableToTypeAndReturn<JObject>(expected[nameof(actual.Channel1Item)]));
            AssertChannelMatchesJson(actual.Channel2Item, Guard.IsNotNullAndAssignableToTypeAndReturn<JObject>(expected[nameof(actual.Channel2Item)]));
        }

        static void AssertChannelMatchesJson(FourierSideChannelSpecularBlockerDTOItem actual, JObject expected)
        {
            using var scope = new AssertionScope(expected.Path);

            ObjectHelper.GetFieldValue(actual, "_rodTotalCount").Should().Be(expected.Value<int>("_rodTotalCount"));
            actual.ChannelId.Should().Be(expected.Value<int>(nameof(actual.ChannelId)));
            actual.Step0FourierImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step0FourierImageFilePath)));
            actual.Step1FourierImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step1FourierImageFilePath)));
            actual.RawStep0CIBImageFilePath.Should().Be(expected.Value<string>(nameof(actual.RawStep0CIBImageFilePath)));
            actual.RawStep1CIBImageFilePath.Should().Be(expected.Value<string>(nameof(actual.RawStep1CIBImageFilePath)));
            actual.Step0CIBImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step0CIBImageFilePath)));
            actual.Step1CIBImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step1CIBImageFilePath)));
            actual.Step0CIBImageAverageValue.Should().Be(expected.Value<double>(nameof(actual.Step0CIBImageAverageValue)));
            actual.Step1CIBImageAverageValue.Should().Be(expected.Value<double>(nameof(actual.Step1CIBImageAverageValue)));
            actual.ExtinctionRatio.Should().Be(expected.Value<double>(nameof(actual.ExtinctionRatio)));

            AssertRodsMatchJson(actual.Rods, Guard.IsNotNullAndAssignableToTypeAndReturn<JArray>(expected[nameof(actual.Rods)]));
        }

        static void AssertRodsMatchJson(FourierSideChannelSpecularBlockerDTOItem.Rod[] actual, JArray expected)
        {
            actual.Select(t => t.Index).Should().BeEquivalentTo(expected.Select(t => t.Value<int>("Index")), "{0} rod indexes", expected.Path);
            foreach (var token in expected) AssertRodMatchesJson(actual.Single(t => t.Index == token.Value<int>("Index")), token);
        }

        static void AssertRodMatchesJson(FourierSideChannelSpecularBlockerDTOItem.Rod actual, JToken expected)
        {
            using var scope = new AssertionScope(expected.Path);

            actual.Index.Should().Be(expected.Value<int>(nameof(actual.Index)));
            actual.IsDeleted.Should().Be(expected.Value<bool>(nameof(actual.IsDeleted)));
            actual.MotorAbsoluteValue.Should().Be(expected.Value<double>(nameof(actual.MotorAbsoluteValue)));
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
    [InlineData(6, 11, false)]
    [InlineData(8, 22, true)]
    [InlineData(9, 33, false)]
    [InlineData(23, 44, true)]
    [InlineData(46, 55, false)]
    public void RandomDto_ShouldMatchEveryField_AfterJsonRoundTrip(int rodTotalCount, int seed, bool useCacheSettings)
    {
        var settings = useCacheSettings ? IgnoreCacheItemPropertiesContractResolver.Settings : new JsonSerializerSettings();

        using var expected = CreateRandomDto(new Random(seed), rodTotalCount);

#pragma warning disable IDE0079
#pragma warning disable IDISP004

        using var actual = Deserialize(JsonConvert.SerializeObject(expected, settings), settings);

#pragma warning restore IDISP004
#pragma warning restore IDE0079

        AssertEquals();

        return;

        static FourierSideChannelSpecularBlockerDTO CreateRandomDto(Random random, int rodTotalCount)
        {
            var dto = new FourierSideChannelSpecularBlockerDTO(rodTotalCount)
            {
                IsCalibrated = random.Next(2) == 1,
                IsVerified = random.Next(2) == 1,
                IsRequiredSelfCheck = random.Next(2) == 1,
                Id = ((long)random.Next() << 31) ^ random.Next(),
                Expiration = ((long)random.Next() << 31) ^ random.Next()
            };

            foreach (var channel in new[] { dto.Channel1Item, dto.Channel2Item })
            {
                channel.Step0FourierImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\Step0Fourier_{random.Next(0, 99)}.jpg";
                channel.Step1FourierImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\Step1Fourier_{random.Next(0, 99)}.jpg";
                channel.RawStep0CIBImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\RawStep0CIB_{random.Next(0, 99)}.jpg";
                channel.RawStep1CIBImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\RawStep1CIB_{random.Next(0, 99)}.jpg";
                channel.Step0CIBImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\Step0CIB_{random.Next(0, 99)}.jpg";
                channel.Step1CIBImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\Step1CIB_{random.Next(0, 99)}.jpg";
                channel.Step0CIBImageAverageValue = Math.Round((random.NextDouble() - 0.5d) * 4000d, 3);
                channel.Step1CIBImageAverageValue = Math.Round((random.NextDouble() - 0.5d) * 4000d, 3);
                channel.ExtinctionRatio = Math.Round((random.NextDouble() - 0.5d) * 4000d, 3);
                foreach (var rod in channel.Rods)
                {
                    rod.IsDeleted = random.Next(2) == 1;
                    rod.ImageROI = random.Next(6) switch
                    {
                        0 => Rect.Empty,
                        1 => new Rect(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), 0d, 0d),
                        _ => new Rect(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Abs(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3)), Math.Abs(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3)))
                    };
                    rod.MotorAbsoluteValue = Math.Round((random.NextDouble() - 0.5d) * 4000d, 3);
                }
            }

            return dto;
        }

        void AssertEquals()
        {
            using var scope = new AssertionScope("DTO");

            ObjectHelper.GetFieldValue(actual, "_rodTotalCount").Should().Be(ObjectHelper.GetFieldValue(expected, "_rodTotalCount"));
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
        }

        static void AssertChannelEquals(FourierSideChannelSpecularBlockerDTOItem actual, FourierSideChannelSpecularBlockerDTOItem expected)
        {
            ObjectHelper.GetFieldValue(actual, "_rodTotalCount").Should().Be(ObjectHelper.GetFieldValue(expected, "_rodTotalCount"));
            actual.ChannelId.Should().Be(expected.ChannelId);
            actual.Step0FourierImageFilePath.Should().Be(expected.Step0FourierImageFilePath);
            actual.Step1FourierImageFilePath.Should().Be(expected.Step1FourierImageFilePath);
            actual.RawStep0CIBImageFilePath.Should().Be(expected.RawStep0CIBImageFilePath);
            actual.RawStep1CIBImageFilePath.Should().Be(expected.RawStep1CIBImageFilePath);
            actual.Step0CIBImageFilePath.Should().Be(expected.Step0CIBImageFilePath);
            actual.Step1CIBImageFilePath.Should().Be(expected.Step1CIBImageFilePath);
            actual.Step0CIBImageAverageValue.Should().Be(expected.Step0CIBImageAverageValue);
            actual.Step1CIBImageAverageValue.Should().Be(expected.Step1CIBImageAverageValue);
            actual.ExtinctionRatio.Should().Be(expected.ExtinctionRatio);
            actual.Rods.Should().Equal(expected.Rods, (t1, t2) => t1.Index == t2.Index);
            foreach (var source in expected.Rods)
            {
                var rod = actual.Rods.Single(t => t.Index == source.Index);
                rod.IsDeleted.Should().Be(source.IsDeleted);
                rod.ImageROI.Should().Be(source.ImageROI);
                rod.MotorAbsoluteValue.Should().Be(source.MotorAbsoluteValue);
            }
        }
    }

    private static FourierSideChannelSpecularBlockerDTO Deserialize(string json, JsonSerializerSettings settings)
    {
        var payload = JObject.Parse(json);
        payload.Remove(nameof(FourierSideChannelSpecularBlockerDTO.ProductivityInformation));

#pragma warning disable IDISP004

        return Guard.IsNotNullAndReturn(JsonConvert.DeserializeObject<FourierSideChannelSpecularBlockerDTO>(payload.ToString(), settings));

#pragma warning restore IDISP004
    }
}