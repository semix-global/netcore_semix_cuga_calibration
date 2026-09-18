using System.Collections.Concurrent;
using System.IO;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Fourier.SideChannelSpecularBlocker;
using Local.SQL.Cache.Providers.Serializations;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Models.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CugaCalibrationUnitTest.Serializations;

[Collection(HostCollection.Name)]
public sealed class FourierSideChannelSpecularBlockerSerializationTest(HostFixture fixture)
{
    private static readonly string[] NonPersistentNames =
    [
        "BitmapImageROIDrawable",
        "_step0FourierBitmapImageDrawable",
        "_step1FourierBitmapImageDrawable",
        "_step1FourierROIDrawables",
        "_step0CIBBitmapImageDrawable",
        "_step1CIBBitmapImageDrawable",
        "CIBDocument",
        "Document"
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

        using var actual = JsonConvert.DeserializeObject<FourierSideChannelSpecularBlockerDTO>(json, settings);
        actual.Should().NotBeNull();
        actual.HasErrors.Should().BeFalse();

        AssertMatchesJson();

        var saved = JObject.Parse(JsonConvert.SerializeObject(actual, settings));
        saved.Descendants().OfType<JProperty>().Select(t => t.Name).Should().NotContain(NonPersistentNames);

        if (useCacheSettings == false)
        {
            RemoveMetadata(saved);
            saved.Remove(nameof(ICacheItem.IsDeleted)); // Rod 的 IsDeleted 不能删除
        }

        saved.ToString(Formatting.Indented).Should().Be(expected.ToString(Formatting.Indented));

        return;

        void AssertMatchesJson()
        {
            using var scope = new AssertionScope(expected.Path);

            ObjectHelper.GetFieldValue(actual, "_rodTotalCount").Should().Be(expected.Value<int>("_rodTotalCount"));

            var productivityJson = expected[nameof(actual.ProductivityInformation)].Should().NotBeNull().And.BeAssignableTo<JToken>().Which;
            actual.ProductivityInformation.OpticsIlluminationModeEnum.Should().Be((OpticsIlluminationModeEnum)productivityJson.Value<int>(nameof(ProductivityInformation.OpticsIlluminationModeEnum)));
            actual.ProductivityInformation.OpticsMagType.Should().Be(productivityJson.Value<int>(nameof(ProductivityInformation.OpticsMagType)));
            actual.ProductivityInformation.StageSpeedType.Should().Be(productivityJson.Value<int>(nameof(ProductivityInformation.StageSpeedType)));

            actual.IsCalibrated.Should().Be(expected.Value<bool>(nameof(actual.IsCalibrated)));
            actual.IsVerified.Should().Be(expected.Value<bool>(nameof(actual.IsVerified)));
            actual.IsRequiredSelfCheck.Should().Be(expected.Value<bool>(nameof(actual.IsRequiredSelfCheck)));
            actual.IsOk.Should().Be(expected.Value<bool>(nameof(actual.IsOk)));

            AssertChannelMatchesJson(actual.Channel1Item, expected[nameof(actual.Channel1Item)].Should().NotBeNull().And.BeAssignableTo<JToken>().Which);
            AssertChannelMatchesJson(actual.Channel2Item, expected[nameof(actual.Channel2Item)].Should().NotBeNull().And.BeAssignableTo<JToken>().Which);
        }

        static void AssertChannelMatchesJson(FourierSideChannelSpecularBlockerDTOItem actual, JToken expected)
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

            AssertRodsMatchJson(actual.Rods, expected[nameof(actual.Rods)].Should().NotBeNull().And.BeAssignableTo<JArray>().Which);
        }

        static void AssertRodsMatchJson(FourierSideChannelSpecularBlockerDTOItem.Rod[] actual, JArray expected)
        {
            actual.Select(t => t.Index).Should().BeEquivalentTo(expected.Select(t => t.Value<int>("Index")), "{0} rod indexes", expected.Path); // 不考虑顺序

            foreach (var token in expected) AssertRodMatchesJson(actual.Single(t => t.Index == token.Value<int>("Index")), token);
        }

        static void AssertRodMatchesJson(FourierSideChannelSpecularBlockerDTOItem.Rod actual, JToken expected)
        {
            using var scope = new AssertionScope(expected.Path);

            actual.Index.Should().Be(expected.Value<int>(nameof(actual.Index)));
            actual.IsDeleted.Should().Be(expected.Value<bool>(nameof(actual.IsDeleted)));
            actual.MotorAbsoluteValue.Should().Be(expected.Value<double>(nameof(actual.MotorAbsoluteValue)));

            AssertRectMatchesJson(actual.ImageROI, expected[nameof(actual.ImageROI)].Should().NotBeNull().And.BeAssignableTo<JToken>().Which);
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

        var productivityInformations = fixture.Host.Services.GetRequiredService<ApplicationCookie>().ProductivityInformations;
        using var expected = CreateRandom(new Random(seed), rodTotalCount, productivityInformations);

        var json = JsonConvert.SerializeObject(expected, settings);

        using var actual = JsonConvert.DeserializeObject<FourierSideChannelSpecularBlockerDTO>(json, settings);
        actual.Should().NotBeNull();
        actual.HasErrors.Should().BeFalse();

        AssertEquals();

        var expectedJson = JObject.Parse(json);
        var saved = JObject.Parse(JsonConvert.SerializeObject(actual, settings));
        saved.Descendants().OfType<JProperty>().Select(t => t.Name).Should().NotContain(NonPersistentNames);
        saved.ToString(Formatting.Indented).Should().Be(expectedJson.ToString(Formatting.Indented));

        foreach (var name in IgnoreProperties)
        {
            saved.ContainsKey(name).Should().Be(useCacheSettings == false, "{0} persistence must follow the selected settings", name);
        }

        saved.ContainsKey(nameof(ICacheItem.IsDeleted)).Should().Be(useCacheSettings == false, "{0} persistence must follow the selected settings", nameof(ICacheItem.IsDeleted));

        return;

        static FourierSideChannelSpecularBlockerDTO CreateRandom(Random random, int rodTotalCount, IReadOnlyList<ProductivityInformation> productivityInformations)
        {
            var dto = new FourierSideChannelSpecularBlockerDTO(rodTotalCount)
            {
                IsCalibrated = random.Next(2) == 1,
                IsVerified = random.Next(2) == 1,
                IsRequiredSelfCheck = random.Next(2) == 1,
                Id = ((long)random.Next() << 31) ^ random.Next(),
                Expiration = ((long)random.Next() << 31) ^ random.Next(),
                IsDeleted = random.Next(2) == 1,
                CreatedUserId = ((long)random.Next() << 31) ^ random.Next(),
                CreatedUserName = $"Created_{random.Next()}",
                CreatedTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(((long)random.Next() << 20) ^ random.Next()),
                ModifiedUserId = ((long)random.Next() << 31) ^ random.Next(),
                ModifiedUserName = $"Modified_{random.Next()}",
            };

            dto.ModifiedTime = dto.CreatedTime.AddTicks(random.Next(1, int.MaxValue));

            dto.ProductivityInformation = productivityInformations[random.Next(productivityInformations.Count)].Clone();

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
                        2 => new Rect(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), 0d, random.NextDouble() * 4000d + 1d),
                        3 => new Rect(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), random.NextDouble() * 4000d + 1d, 0d),
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

            actual.ProductivityInformation.Should().Be(expected.ProductivityInformation);
            actual.ProductivityInformation.OpticsIlluminationModeEnum.Should().Be(expected.ProductivityInformation.OpticsIlluminationModeEnum);
            actual.ProductivityInformation.OpticsMagType.Should().Be(expected.ProductivityInformation.OpticsMagType);
            actual.ProductivityInformation.StageSpeedType.Should().Be(expected.ProductivityInformation.StageSpeedType);

            actual.IsCalibrated.Should().Be(expected.IsCalibrated);
            actual.IsVerified.Should().Be(expected.IsVerified);
            actual.IsRequiredSelfCheck.Should().Be(expected.IsRequiredSelfCheck);
            actual.IsOk.Should().Be(expected.IsOk);

            actual.Id.Should().Be(useCacheSettings ? 0L : expected.Id);
            actual.Expiration.Should().Be(useCacheSettings ? 0L : expected.Expiration);
            actual.IsDeleted.Should().Be(useCacheSettings == false && expected.IsDeleted);
            actual.CreatedUserId.Should().Be(useCacheSettings ? 0L : expected.CreatedUserId);
            actual.CreatedUserName.Should().Be(useCacheSettings ? string.Empty : expected.CreatedUserName);
            actual.CreatedTime.Should().Be(useCacheSettings ? default : expected.CreatedTime);
            actual.ModifiedUserId.Should().Be(useCacheSettings ? 0L : expected.ModifiedUserId);
            actual.ModifiedUserName.Should().Be(useCacheSettings ? string.Empty : expected.ModifiedUserName);
            actual.ModifiedTime.Should().Be(useCacheSettings ? default : expected.ModifiedTime);

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

                rod.Index.Should().Be(source.Index);
                rod.IsDeleted.Should().Be(source.IsDeleted);
                rod.ImageROI.Should().Be(source.ImageROI);
                rod.MotorAbsoluteValue.Should().Be(source.MotorAbsoluteValue);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cache_ShouldMatchEveryField_AfterJsonRoundTrip(bool useCacheSettings)
    {
        var settings = useCacheSettings ? IgnoreCacheItemPropertiesContractResolver.Settings : new JsonSerializerSettings();

        var cookie = fixture.Host.Services.GetRequiredService<ApplicationCookie>();
        var oiItem = new FourierSideChannelSpecularBlockerCacheItem
        {
            MicroscopeLensInformation = cookie.MicroscopeLensInformations[0],
            LaserLightInformation = cookie.LaserLightInformations[0],
            OpticsConfiguration = new OpticsConfiguration
            {
                OpticsApodizationModeEnum = OpticsApodizationModeEnum.Cosine,
                OpticsPolarizationModeEnum = OpticsPolarizationModeEnum.P,
                OpticsCollectorPolarizationModeEnum = OpticsCollectorPolarizationModeEnum.N
            },
            ScanLength = 510.5d,
            ShinyWaferFindBFMachinePosition = new Point(3.5, 4.5),
            AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum.Sharpe,
            AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum.Size32,
            Id = 51,
            Expiration = 52,
            IsDeleted = false,
            CreatedUserId = 3001,
            CreatedUserName = "Created_Specular_OI",
            CreatedTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ModifiedUserId = 3002,
            ModifiedUserName = "Modified_Specular_OI"
        };
        oiItem.ModifiedTime = oiItem.CreatedTime.AddTicks(7);

        var niItem = new FourierSideChannelSpecularBlockerCacheItem
        {
            MicroscopeLensInformation = cookie.MicroscopeLensInformations[^1],
            LaserLightInformation = cookie.LaserLightInformations[^1],
            OpticsConfiguration = new OpticsConfiguration
            {
                OpticsApodizationModeEnum = OpticsApodizationModeEnum.None,
                OpticsPolarizationModeEnum = OpticsPolarizationModeEnum.S,
                OpticsCollectorPolarizationModeEnum = OpticsCollectorPolarizationModeEnum.None
            },
            ScanLength = 720.25d,
            ShinyWaferFindBFMachinePosition = new Point(13.5, 14.5),
            AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum.Ncc,
            AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum.Size256,
            Id = 53,
            Expiration = 54,
            IsDeleted = true,
            CreatedUserId = 3003,
            CreatedUserName = "Created_Specular_NI",
            CreatedTime = new DateTime(2001, 2, 2, 0, 0, 0, DateTimeKind.Utc),
            ModifiedUserId = 3004,
            ModifiedUserName = "Modified_Specular_NI"
        };
        niItem.ModifiedTime = niItem.CreatedTime.AddTicks(9);

        var expected = new FourierSideChannelSpecularBlockerCache
        {
            ProductivityInformation = cookie.OIProductivityInformations[0],
            VerifyLaserLightInformation = cookie.LaserLightInformations[0],
            VerifyCIBConfiguration = new CIBConfiguration
            {
                Gain = 4,
                IsAutoGainControl = false,
                IsL0K = true,
                CIBProfileMode = CIBProfileModeEnum.PMTLog,
                IsKeepRawImageCIBProfileModeEnum = true
            },
            VerifyImageWidth = 2048,
            ExtinctionRatioThreshold = 0.35d,
            AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum.Sharpe,
            AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum.Size512,
            Id = 61,
            Expiration = 62,
            IsDeleted = true,
            CreatedUserId = 3101,
            CreatedUserName = "Created_Specular",
            CreatedTime = new DateTime(2000, 3, 3, 0, 0, 0, DateTimeKind.Utc),
            ModifiedUserId = 3102,
            ModifiedUserName = "Modified_Specular",
            Items = new ConcurrentDictionary<ProductivityInformation, FourierSideChannelSpecularBlockerCacheItem>
            {
                [cookie.OIProductivityInformations[0]] = oiItem,
                [cookie.NIProductivityInformations[0]] = niItem
            }
        };
        expected.ModifiedTime = expected.CreatedTime.AddTicks(11);

        var json = JsonConvert.SerializeObject(expected, settings);
        var actual = JsonConvert.DeserializeObject<FourierSideChannelSpecularBlockerCache>(json, settings);

        actual.Should().NotBeNull();
        actual.HasErrors.Should().BeFalse();

        actual.ProductivityInformation.Should().Be(expected.ProductivityInformation);
        actual.VerifyLaserLightInformation.Should().Be(expected.VerifyLaserLightInformation);
        actual.VerifyCIBConfiguration.Gain.Should().Be(expected.VerifyCIBConfiguration.Gain);
        actual.VerifyCIBConfiguration.IsAutoGainControl.Should().Be(expected.VerifyCIBConfiguration.IsAutoGainControl);
        actual.VerifyCIBConfiguration.IsL0K.Should().Be(expected.VerifyCIBConfiguration.IsL0K);
        actual.VerifyCIBConfiguration.CIBProfileMode.Should().Be(expected.VerifyCIBConfiguration.CIBProfileMode);
        actual.VerifyCIBConfiguration.IsKeepRawImageCIBProfileModeEnum.Should().Be(expected.VerifyCIBConfiguration.IsKeepRawImageCIBProfileModeEnum);
        actual.VerifyImageWidth.Should().Be(expected.VerifyImageWidth);
        actual.ExtinctionRatioThreshold.Should().Be(expected.ExtinctionRatioThreshold);
        actual.AlgorithmTemplateTypeEnum.Should().Be(expected.AlgorithmTemplateTypeEnum);
        actual.AlgorithmTemplateSizeEnum.Should().Be(expected.AlgorithmTemplateSizeEnum);

        actual.Id.Should().Be(useCacheSettings ? 0L : expected.Id);
        actual.Expiration.Should().Be(useCacheSettings ? 0L : expected.Expiration);
        actual.IsDeleted.Should().Be(useCacheSettings == false && expected.IsDeleted);
        actual.CreatedUserId.Should().Be(useCacheSettings ? 0L : expected.CreatedUserId);
        actual.CreatedUserName.Should().Be(useCacheSettings ? string.Empty : expected.CreatedUserName);
        actual.CreatedTime.Should().Be(useCacheSettings ? default : expected.CreatedTime);
        actual.ModifiedUserId.Should().Be(useCacheSettings ? 0L : expected.ModifiedUserId);
        actual.ModifiedUserName.Should().Be(useCacheSettings ? string.Empty : expected.ModifiedUserName);
        actual.ModifiedTime.Should().Be(useCacheSettings ? default : expected.ModifiedTime);

        actual.Items.Should().HaveCount(2);
        AssertItemEquals(actual.Items.Single(i => i.Key == cookie.OIProductivityInformations[0]).Value, oiItem);
        AssertItemEquals(actual.Items.Single(i => i.Key == cookie.NIProductivityInformations[0]).Value, niItem);

        JsonConvert.SerializeObject(actual, settings).Should().Be(json);

        return;

        void AssertItemEquals(FourierSideChannelSpecularBlockerCacheItem actualItem, FourierSideChannelSpecularBlockerCacheItem expectedItem)
        {
            actualItem.HasErrors.Should().BeFalse();
            actualItem.MicroscopeLensInformation.Should().Be(expectedItem.MicroscopeLensInformation);
            actualItem.LaserLightInformation.Should().Be(expectedItem.LaserLightInformation);
            actualItem.OpticsConfiguration.OpticsApodizationModeEnum.Should().Be(expectedItem.OpticsConfiguration.OpticsApodizationModeEnum);
            actualItem.OpticsConfiguration.OpticsPolarizationModeEnum.Should().Be(expectedItem.OpticsConfiguration.OpticsPolarizationModeEnum);
            actualItem.OpticsConfiguration.OpticsCollectorPolarizationModeEnum.Should().Be(expectedItem.OpticsConfiguration.OpticsCollectorPolarizationModeEnum);
            actualItem.ScanLength.Should().Be(expectedItem.ScanLength);
            actualItem.ShinyWaferFindBFMachinePosition.Should().Be(expectedItem.ShinyWaferFindBFMachinePosition);
            actualItem.AlgorithmTemplateTypeEnum.Should().Be(expectedItem.AlgorithmTemplateTypeEnum);
            actualItem.AlgorithmTemplateSizeEnum.Should().Be(expectedItem.AlgorithmTemplateSizeEnum);

            actualItem.Id.Should().Be(useCacheSettings ? 0L : expectedItem.Id);
            actualItem.Expiration.Should().Be(useCacheSettings ? 0L : expectedItem.Expiration);
            actualItem.IsDeleted.Should().Be(useCacheSettings == false && expectedItem.IsDeleted);
            actualItem.CreatedUserId.Should().Be(useCacheSettings ? 0L : expectedItem.CreatedUserId);
            actualItem.CreatedUserName.Should().Be(useCacheSettings ? string.Empty : expectedItem.CreatedUserName);
            actualItem.CreatedTime.Should().Be(useCacheSettings ? default : expectedItem.CreatedTime);
            actualItem.ModifiedUserId.Should().Be(useCacheSettings ? 0L : expectedItem.ModifiedUserId);
            actualItem.ModifiedUserName.Should().Be(useCacheSettings ? string.Empty : expectedItem.ModifiedUserName);
            actualItem.ModifiedTime.Should().Be(useCacheSettings ? default : expectedItem.ModifiedTime);
        }
    }
}