using AwesomeAssertions;
using AwesomeAssertions.Execution;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Fourier.V0CenterChannelSpecularBlocker;
using Local.SQL.Cache.Providers.Serializations;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Net.Utilities.Models.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.IO;

namespace CugaCalibrationUnitTest.Serializations;

[Collection(HostCollection.Name)]
public sealed class V0FourierCenterChannelSpecularBlockerSerializationTest(HostFixture fixture)
{
    private static readonly string[] NonPersistentNames =
    [
        "_step0FourierBitmapImageDrawable",
        "_step1FourierBitmapImageDrawable",
        "_step1YRodBitmapImageROIDrawable",
        "_step1XPushBitmapImageROIDrawable",
        "_step0CIBBitmapImageDrawable",
        "_step1CIBBitmapImageDrawable",
        "BitmapImageROIDrawable",
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

        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "FourierSerialization", "V0FourierCenterChannelSpecularBlocker.json"));
        var expected = JObject.Parse(json);

        using var actual = JsonConvert.DeserializeObject<V0FourierCenterChannelSpecularBlockerDTO>(json, settings);
        actual.Should().NotBeNull();
        actual.HasErrors.Should().BeFalse();

        AssertMatchesJson();

        var saved = JObject.Parse(JsonConvert.SerializeObject(actual, settings));
        saved.Descendants().OfType<JProperty>().Select(t => t.Name).Should().NotContain(NonPersistentNames);

        if (useCacheSettings == false)
        {
            RemoveMetadata(saved);
            saved.Remove(nameof(ICacheItem.IsDeleted));
        }

        saved.ToString(Formatting.Indented).Should().Be(expected.ToString(Formatting.Indented));

        return;

        void AssertMatchesJson()
        {
            using var scope = new AssertionScope(expected.Path);

            var productivityJson = expected[nameof(actual.ProductivityInformation)].Should().NotBeNull().And.BeAssignableTo<JToken>().Which;
            actual.ProductivityInformation.OpticsIlluminationModeEnum.Should().Be((OpticsIlluminationModeEnum)productivityJson.Value<int>(nameof(ProductivityInformation.OpticsIlluminationModeEnum)));
            actual.ProductivityInformation.OpticsMagType.Should().Be(productivityJson.Value<int>(nameof(ProductivityInformation.OpticsMagType)));
            actual.ProductivityInformation.StageSpeedType.Should().Be(productivityJson.Value<int>(nameof(ProductivityInformation.StageSpeedType)));

            actual.IsCalibrated.Should().Be(expected.Value<bool>(nameof(actual.IsCalibrated)));
            actual.IsVerified.Should().Be(expected.Value<bool>(nameof(actual.IsVerified)));
            actual.IsRequiredSelfCheck.Should().Be(expected.Value<bool>(nameof(actual.IsRequiredSelfCheck)));
            actual.IsOk.Should().Be(expected.Value<bool>(nameof(actual.IsOk)));

            AssertItemMatchesJson(actual.Channel3Item, expected[nameof(actual.Channel3Item)].Should().NotBeNull().And.BeAssignableTo<JToken>().Which);
        }

        static void AssertItemMatchesJson(V0FourierCenterChannelSpecularBlockerDTOItem actual, JToken expected)
        {
            using var scope = new AssertionScope(expected.Path);

            actual.YRodAngleMotorAbsoluteValue.Should().Be(expected.Value<double>(nameof(actual.YRodAngleMotorAbsoluteValue)));
            actual.Step0FourierImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step0FourierImageFilePath)));
            actual.Step1FourierImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step1FourierImageFilePath)));
            actual.RawStep0CIBImageFilePath.Should().Be(expected.Value<string>(nameof(actual.RawStep0CIBImageFilePath)));
            actual.RawStep1CIBImageFilePath.Should().Be(expected.Value<string>(nameof(actual.RawStep1CIBImageFilePath)));
            actual.Step0CIBImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step0CIBImageFilePath)));
            actual.Step1CIBImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step1CIBImageFilePath)));
            actual.Step0CIBImageAverageValue.Should().Be(expected.Value<double>(nameof(actual.Step0CIBImageAverageValue)));
            actual.Step1CIBImageAverageValue.Should().Be(expected.Value<double>(nameof(actual.Step1CIBImageAverageValue)));
            actual.ExtinctionRatio.Should().Be(expected.Value<double>(nameof(actual.ExtinctionRatio)));

            AssertROIMatchesJson(actual.YRodROI, expected[nameof(actual.YRodROI)].Should().NotBeNull().And.BeAssignableTo<JToken>().Which);
            AssertROIMatchesJson(actual.XPushROI, expected[nameof(actual.XPushROI)].Should().NotBeNull().And.BeAssignableTo<JToken>().Which);
        }

        static void AssertROIMatchesJson(V0FourierCenterChannelSpecularBlockerDTO.ROI actual, JToken expected)
        {
            using var scope = new AssertionScope(expected.Path);

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
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(11, false)]
    [InlineData(22, true)]
    [InlineData(33, false)]
    [InlineData(44, true)]
    [InlineData(55, false)]
    public void RandomDto_ShouldMatchEveryField_AfterJsonRoundTrip(int seed, bool useCacheSettings)
    {
        var settings = useCacheSettings ? IgnoreCacheItemPropertiesContractResolver.Settings : new JsonSerializerSettings();

        var productivityInformations = fixture.Host.Services.GetRequiredService<ApplicationCookie>().ProductivityInformations;
        using var expected = CreateRandom(new Random(seed), productivityInformations);

        var json = JsonConvert.SerializeObject(expected, settings);

        using var actual = JsonConvert.DeserializeObject<V0FourierCenterChannelSpecularBlockerDTO>(json, settings);
        actual.Should().NotBeNull();
        actual.HasErrors.Should().BeFalse();

        AssertEquals(actual, expected, useCacheSettings);
        AssertIndependentItem(actual.Channel3Item, expected.Channel3Item);

        using var cloned = expected.Clone();
        cloned.Should().NotBeNull();
        cloned.Should().NotBeSameAs(expected);
        cloned.HasErrors.Should().BeFalse();
        AssertIndependentItem(cloned.Channel3Item, expected.Channel3Item);
        AssertEquals(cloned, expected, useCacheSettings: false, isClone: true);

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

        static V0FourierCenterChannelSpecularBlockerDTO CreateRandom(Random random, IReadOnlyList<ProductivityInformation> productivityInformations)
        {
            var dto = new V0FourierCenterChannelSpecularBlockerDTO
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

            var channel = dto.Channel3Item;
            channel.YRodAngleMotorAbsoluteValue = new[] { 0d, 120d, 180d, 240d, 300d }[random.Next(5)];
            channel.YRodROI.MotorAbsoluteValue = Math.Round((random.NextDouble() - 0.5d) * 40d, 3);
            channel.XPushROI.MotorAbsoluteValue = Math.Round((random.NextDouble() - 0.5d) * 40d, 3);
            channel.YRodROI.ImageROI = new Rect(Math.Round(random.NextDouble() * 100d, 3), Math.Round(random.NextDouble() * 100d, 3), Math.Round(random.NextDouble() * 80d + 1d, 3), Math.Round(random.NextDouble() * 40d + 1d, 3));
            channel.XPushROI.ImageROI = new Rect(Math.Round(random.NextDouble() * 100d, 3), Math.Round(random.NextDouble() * 100d, 3), Math.Round(random.NextDouble() * 40d + 1d, 3), Math.Round(random.NextDouble() * 80d + 1d, 3));
            channel.Step0FourierImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\Step0Fourier_{random.Next(0, 99)}.jpg";
            channel.Step1FourierImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\Step1Fourier_{random.Next(0, 99)}.jpg";
            channel.RawStep0CIBImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\RawStep0CIB_{random.Next(0, 99)}.jpg";
            channel.RawStep1CIBImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\RawStep1CIB_{random.Next(0, 99)}.jpg";
            channel.Step0CIBImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\Step0CIB_{random.Next(0, 99)}.jpg";
            channel.Step1CIBImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\Step1CIB_{random.Next(0, 99)}.jpg";
            channel.Step0CIBImageAverageValue = Math.Round((random.NextDouble() - 0.5d) * 4000d, 3);
            channel.Step1CIBImageAverageValue = Math.Round((random.NextDouble() - 0.5d) * 4000d, 3);
            channel.ExtinctionRatio = Math.Round((random.NextDouble() - 0.5d) * 4000d, 3);

            return dto;
        }

        static void AssertEquals(V0FourierCenterChannelSpecularBlockerDTO actual, V0FourierCenterChannelSpecularBlockerDTO expected, bool useCacheSettings, bool isClone = false)
        {
            using var scope = new AssertionScope("DTO");

            var persistAudit = useCacheSettings == false && isClone == false;

            actual.ProductivityInformation.Should().Be(expected.ProductivityInformation);
            actual.ProductivityInformation.OpticsIlluminationModeEnum.Should().Be(expected.ProductivityInformation.OpticsIlluminationModeEnum);
            actual.ProductivityInformation.OpticsMagType.Should().Be(expected.ProductivityInformation.OpticsMagType);
            actual.ProductivityInformation.StageSpeedType.Should().Be(expected.ProductivityInformation.StageSpeedType);

            actual.IsCalibrated.Should().Be(expected.IsCalibrated);
            actual.IsVerified.Should().Be(expected.IsVerified);
            actual.IsRequiredSelfCheck.Should().Be(expected.IsRequiredSelfCheck);
            actual.IsOk.Should().Be(expected.IsOk);

            actual.Id.Should().Be(useCacheSettings && isClone == false ? 0L : expected.Id);
            actual.Expiration.Should().Be(useCacheSettings && isClone == false ? 0L : expected.Expiration);
            actual.IsDeleted.Should().Be(persistAudit && expected.IsDeleted);
            actual.CreatedUserId.Should().Be(persistAudit ? expected.CreatedUserId : 0L);
            actual.CreatedUserName.Should().Be(persistAudit ? expected.CreatedUserName : string.Empty);
            actual.CreatedTime.Should().Be(persistAudit ? expected.CreatedTime : default);
            actual.ModifiedUserId.Should().Be(persistAudit ? expected.ModifiedUserId : 0L);
            actual.ModifiedUserName.Should().Be(persistAudit ? expected.ModifiedUserName : string.Empty);
            actual.ModifiedTime.Should().Be(persistAudit ? expected.ModifiedTime : default);

            AssertItemEquals(actual.Channel3Item, expected.Channel3Item);
        }

        static void AssertItemEquals(V0FourierCenterChannelSpecularBlockerDTOItem actual, V0FourierCenterChannelSpecularBlockerDTOItem expected)
        {
            actual.YRodAngleMotorAbsoluteValue.Should().Be(expected.YRodAngleMotorAbsoluteValue);
            actual.YRodROI.ImageROI.Should().Be(expected.YRodROI.ImageROI);
            actual.YRodROI.MotorAbsoluteValue.Should().Be(expected.YRodROI.MotorAbsoluteValue);
            actual.XPushROI.ImageROI.Should().Be(expected.XPushROI.ImageROI);
            actual.XPushROI.MotorAbsoluteValue.Should().Be(expected.XPushROI.MotorAbsoluteValue);
            actual.Step0FourierImageFilePath.Should().Be(expected.Step0FourierImageFilePath);
            actual.Step1FourierImageFilePath.Should().Be(expected.Step1FourierImageFilePath);
            actual.RawStep0CIBImageFilePath.Should().Be(expected.RawStep0CIBImageFilePath);
            actual.RawStep1CIBImageFilePath.Should().Be(expected.RawStep1CIBImageFilePath);
            actual.Step0CIBImageFilePath.Should().Be(expected.Step0CIBImageFilePath);
            actual.Step1CIBImageFilePath.Should().Be(expected.Step1CIBImageFilePath);
            actual.Step0CIBImageAverageValue.Should().Be(expected.Step0CIBImageAverageValue);
            actual.Step1CIBImageAverageValue.Should().Be(expected.Step1CIBImageAverageValue);
            actual.ExtinctionRatio.Should().Be(expected.ExtinctionRatio);
        }
    }

    private static void AssertIndependentItem(V0FourierCenterChannelSpecularBlockerDTOItem actual, V0FourierCenterChannelSpecularBlockerDTOItem expected)
    {
        actual.Should().NotBeSameAs(expected);
        actual.YRodROI.Should().NotBeSameAs(expected.YRodROI);
        actual.XPushROI.Should().NotBeSameAs(expected.XPushROI);
        actual.YRodROI.BitmapImageROIDrawable.Should().NotBeSameAs(expected.YRodROI.BitmapImageROIDrawable);
        actual.XPushROI.BitmapImageROIDrawable.Should().NotBeSameAs(expected.XPushROI.BitmapImageROIDrawable);
        actual.Document.Should().NotBeSameAs(expected.Document);
        actual.CIBDocument.Should().NotBeSameAs(expected.CIBDocument);
    }

    [Theory]
    [InlineData(0d, 0d, 42.25d)]
    [InlineData(180d, 5.5d, 43d)]
    public void AdaptTo_ShouldUseAngleAndROIMotorPositions(double angle, double yPosition, double pushPosition)
    {
        using var dto = new V0FourierCenterChannelSpecularBlockerDTO();
        dto.Channel3Item.YRodAngleMotorAbsoluteValue = angle;
        dto.Channel3Item.YRodROI.MotorAbsoluteValue = yPosition;
        dto.Channel3Item.XPushROI.MotorAbsoluteValue = pushPosition;

        var actual = dto.AdaptTo();

        actual.Ch3Angle.Should().Be((float)angle);
        actual.Ch3TurnY.Should().Be((float)yPosition);
        actual.Ch3Push.Should().Be((float)pushPosition);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cache_ShouldMatchEveryField_AfterJsonRoundTrip(bool useCacheSettings)
    {
        var settings = useCacheSettings ? IgnoreCacheItemPropertiesContractResolver.Settings : new JsonSerializerSettings();

        var cookie = fixture.Host.Services.GetRequiredService<ApplicationCookie>();
        var oiItem = new V0FourierCenterChannelSpecularBlockerCacheItem
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

        var niItem = new V0FourierCenterChannelSpecularBlockerCacheItem
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

        var expected = new V0FourierCenterChannelSpecularBlockerCache
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
            Items = new ConcurrentDictionary<ProductivityInformation, V0FourierCenterChannelSpecularBlockerCacheItem>
            {
                [cookie.OIProductivityInformations[0]] = oiItem,
                [cookie.NIProductivityInformations[0]] = niItem
            }
        };
        expected.ModifiedTime = expected.CreatedTime.AddTicks(11);

        var json = JsonConvert.SerializeObject(expected, settings);
        var actual = JsonConvert.DeserializeObject<V0FourierCenterChannelSpecularBlockerCache>(json, settings);

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

        void AssertItemEquals(V0FourierCenterChannelSpecularBlockerCacheItem actualItem, V0FourierCenterChannelSpecularBlockerCacheItem expectedItem)
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