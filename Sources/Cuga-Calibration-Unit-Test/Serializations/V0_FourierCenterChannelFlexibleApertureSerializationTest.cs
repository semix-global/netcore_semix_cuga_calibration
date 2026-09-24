using AwesomeAssertions;
using AwesomeAssertions.Execution;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Fourier.V0CenterChannelFlexibleAperture;
using Local.SQL.Cache.Providers.Serializations;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Models.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.IO;
using System.Reflection;

namespace CugaCalibrationUnitTest.Serializations;

[Collection(HostCollection.Name)]
public sealed class V0FourierCenterChannelFlexibleApertureSerializationTest(HostFixture fixture)
{
    private static readonly string[] NonPersistentNames =
    [
        "BitmapImageROIDrawable",
        "_step0BitmapImageDrawable",
        "_step1BitmapImageDrawable",
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
        nameof(ICacheItem.IsDeleted),
        nameof(ObservableValidator.HasErrors)
    ];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProvidedJson_ShouldMatchEveryField_AndSerializeBackIdentically(bool useCacheSettings)
    {
        var settings = useCacheSettings ? IgnoreCacheItemPropertiesContractResolver.Settings : PrivateSetterContractResolver.Settings;

        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "FourierSerialization", "V0FourierCenterChannelFlexibleAperture.json"));
        var expected = JObject.Parse(json);

        using var actual = JsonConvert.DeserializeObject<V0FourierCenterChannelFlexibleApertureDTO>(json, settings);
        actual.Should().NotBeNull();
        actual.HasErrors.Should().BeFalse();

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

            AssertItemMatchesJson(actual.XApertureItem, expected[nameof(actual.XApertureItem)].Should().NotBeNull().And.BeAssignableTo<JToken>().Which);
            AssertItemsMatchJson(actual.XRodItems, expected[nameof(actual.XRodItems)].Should().NotBeNull().And.BeAssignableTo<JArray>().Which);
            AssertItemMatchesJson(actual.XPushItem, expected[nameof(actual.XPushItem)].Should().NotBeNull().And.BeAssignableTo<JToken>().Which);
            AssertItemMatchesJson(actual.YApertureItem, expected[nameof(actual.YApertureItem)].Should().NotBeNull().And.BeAssignableTo<JToken>().Which);
            AssertItemsMatchJson(actual.YRodItems, expected[nameof(actual.YRodItems)].Should().NotBeNull().And.BeAssignableTo<JArray>().Which);
        }

        static void AssertItemsMatchJson(V0FourierCenterChannelFlexibleApertureDTOItem[] actual, JArray expected)
        {
            actual.Should().HaveCount(expected.Count);

            for (var i = 0; i < actual.Length; i++) AssertItemMatchesJson(actual[i], expected[i]);
        }

        static void AssertItemMatchesJson(V0FourierCenterChannelFlexibleApertureDTOItem actual, JToken expected)
        {
            using var scope = new AssertionScope(expected.Path);

            actual.CenterChannelMotionKindEnum.Should().Be((CenterChannelMotionKindEnum)expected.Value<int>(nameof(actual.CenterChannelMotionKindEnum)));
            actual.DefaultAngleMotorAbsoluteValue.Should().Be(expected.Value<double>(nameof(actual.DefaultAngleMotorAbsoluteValue)));
            actual.AngleMotorAbsoluteValue.Should().Be(expected.Value<double>(nameof(actual.AngleMotorAbsoluteValue)));
            actual.Step0MotorAbsoluteValue.Should().Be(expected.Value<double>(nameof(actual.Step0MotorAbsoluteValue)));
            actual.Step1MotorAbsoluteValue.Should().Be(expected.Value<double>(nameof(actual.Step1MotorAbsoluteValue)));
            actual.Step0ChannelImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step0ChannelImageFilePath)));
            actual.Step1ChannelImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step1ChannelImageFilePath)));
            actual.PixelSize.Should().Be(expected.Value<double>(nameof(actual.PixelSize)));

            AssertRectMatchesJson(actual.Step0ROI.ImageROI, expected[nameof(actual.Step0ROI)]![nameof(actual.Step0ROI.ImageROI)].Should().NotBeNull().And.BeAssignableTo<JToken>().Which);
            AssertRectMatchesJson(actual.Step1ROI.ImageROI, expected[nameof(actual.Step1ROI)]![nameof(actual.Step1ROI.ImageROI)].Should().NotBeNull().And.BeAssignableTo<JToken>().Which);
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
        var settings = useCacheSettings ? IgnoreCacheItemPropertiesContractResolver.Settings : PrivateSetterContractResolver.Settings;

        using var expected = seed == 0
            ? new V0FourierCenterChannelFlexibleApertureDTO()
            : CreateRandom(new Random(seed));

        var json = JsonConvert.SerializeObject(expected, settings);

        using var actual = JsonConvert.DeserializeObject<V0FourierCenterChannelFlexibleApertureDTO>(json, settings);
        actual.Should().NotBeNull();
        actual.HasErrors.Should().BeFalse();

        AssertEquals(actual, expected, useCacheSettings);

        using var cloned = expected.Clone();
        cloned.Should().NotBeNull();
        cloned.Should().NotBeSameAs(expected);
        cloned.HasErrors.Should().BeFalse();
        cloned.XApertureItem.Should().NotBeSameAs(expected.XApertureItem);
        cloned.XPushItem.Should().NotBeSameAs(expected.XPushItem);
        cloned.YApertureItem.Should().NotBeSameAs(expected.YApertureItem);
        cloned.XRodItems.Should().NotBeSameAs(expected.XRodItems);
        cloned.YRodItems.Should().NotBeSameAs(expected.YRodItems);
        AssertEquals(cloned, expected, useCacheSettings: false, isClone: true);

        var expectedJson = JObject.Parse(json);
        var saved = JObject.Parse(JsonConvert.SerializeObject(actual, settings));
        saved.Descendants().OfType<JProperty>().Select(t => t.Name).Should().NotContain(NonPersistentNames);
        saved.ToString(Formatting.Indented).Should().Be(expectedJson.ToString(Formatting.Indented));

        foreach (var name in IgnoreProperties)
        {
            saved.ContainsKey(name).Should().Be(useCacheSettings == false, "{0} persistence must follow the selected settings", name);
        }

        return;

        static V0FourierCenterChannelFlexibleApertureDTO CreateRandom(Random random)
        {
            var dto = new V0FourierCenterChannelFlexibleApertureDTO
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

            foreach (var item in new[] { dto.XApertureItem, dto.XPushItem, dto.YApertureItem }.Concat(dto.XRodItems).Concat(dto.YRodItems))
            {
                item.AngleMotorAbsoluteValue = Math.Round((random.NextDouble() - 0.5d) * 4000d, 3);
                item.Step0MotorAbsoluteValue = Math.Round((random.NextDouble() - 0.5d) * 4000d, 3);
                item.Step1MotorAbsoluteValue = Math.Round((random.NextDouble() - 0.5d) * 4000d, 3);
                item.Step0ChannelImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\step_{random.Next(0, 99)}.jpg";
                item.Step1ChannelImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\step_{random.Next(0, 99)}.jpg";
                item.Step0ROI.ImageROI = CreateRandomRect(random);
                item.Step1ROI.ImageROI = CreateRandomRect(random);
                ObjectHelper.SetPropertyValue(item, nameof(V0FourierCenterChannelFlexibleApertureDTOItem.PixelSize), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3));
            }

            return dto;

            static Rect CreateRandomRect(Random random) => random.Next(6) switch
            {
                0 => Rect.Empty,
                1 => new Rect(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), 0d, 0d),
                2 => new Rect(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), 0d, random.NextDouble() * 4000d + 1d),
                3 => new Rect(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), random.NextDouble() * 4000d + 1d, 0d),
                _ => new Rect(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Abs(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3)), Math.Abs(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3)))
            };
        }

        static void AssertEquals(V0FourierCenterChannelFlexibleApertureDTO actual, V0FourierCenterChannelFlexibleApertureDTO expected, bool useCacheSettings, bool isClone = false)
        {
            using var scope = new AssertionScope("DTO");

            var persistAudit = useCacheSettings == false && isClone == false;

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

            AssertItemEquals(actual.XApertureItem, expected.XApertureItem);
            AssertItemEquals(actual.XPushItem, expected.XPushItem);
            AssertItemEquals(actual.YApertureItem, expected.YApertureItem);
            actual.XRodItems.Should().HaveCount(expected.XRodItems.Length);
            actual.YRodItems.Should().HaveCount(expected.YRodItems.Length);

            for (var i = 0; i < expected.XRodItems.Length; i++) AssertItemEquals(actual.XRodItems[i], expected.XRodItems[i]);
            for (var i = 0; i < expected.YRodItems.Length; i++) AssertItemEquals(actual.YRodItems[i], expected.YRodItems[i]);
        }

        static void AssertItemEquals(V0FourierCenterChannelFlexibleApertureDTOItem actual, V0FourierCenterChannelFlexibleApertureDTOItem expected)
        {
            actual.CenterChannelMotionKindEnum.Should().Be(expected.CenterChannelMotionKindEnum);
            actual.DefaultAngleMotorAbsoluteValue.Should().Be(expected.DefaultAngleMotorAbsoluteValue);
            actual.AngleMotorAbsoluteValue.Should().Be(expected.AngleMotorAbsoluteValue);
            actual.Step0MotorAbsoluteValue.Should().Be(expected.Step0MotorAbsoluteValue);
            actual.Step1MotorAbsoluteValue.Should().Be(expected.Step1MotorAbsoluteValue);
            actual.Step0ChannelImageFilePath.Should().Be(expected.Step0ChannelImageFilePath);
            actual.Step1ChannelImageFilePath.Should().Be(expected.Step1ChannelImageFilePath);
            actual.PixelSize.Should().Be(expected.PixelSize);
            actual.Step0ROI.ImageROI.Should().Be(expected.Step0ROI.ImageROI);
            actual.Step1ROI.ImageROI.Should().Be(expected.Step1ROI.ImageROI);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cache_ShouldMatchEveryField_AfterJsonRoundTrip(bool useCacheSettings)
    {
        var settings = useCacheSettings ? IgnoreCacheItemPropertiesContractResolver.Settings : new JsonSerializerSettings();

        var cookie = fixture.Host.Services.GetRequiredService<ApplicationCookie>();
        var expected = new V0FourierCenterChannelFlexibleApertureCache
        {
            XMinMotorAbsoluteValue = -100.5d,
            XMaxMotorAbsoluteValue = 100.25d,
            PushMinMotorAbsoluteValue = 0d,
            PushMaxMotorAbsoluteValue = 80.5d,
            YMinMotorAbsoluteValue = -20.25d,
            YMaxMotorAbsoluteValue = 90.75d,
            ProductivityInformation = cookie.ProductivityInformations[0],
            MicroscopeLensInformation = cookie.MicroscopeLensInformations[0],
            LaserLightInformation = cookie.LaserLightInformations[0],
            OpticsConfiguration = new OpticsConfiguration
            {
                OpticsApodizationModeEnum = OpticsApodizationModeEnum.SuperGaussian,
                OpticsPolarizationModeEnum = OpticsPolarizationModeEnum.C,
                OpticsCollectorPolarizationModeEnum = OpticsCollectorPolarizationModeEnum.S
            },
            ScanLength = 500.25d,
            HazeFindBFMachinePosition = new Point(11.5, 21.5),
            AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum.Ncc,
            AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum.Size128,
            Id = 31,
            Expiration = 32,
            IsDeleted = true,
            CreatedUserId = 2001,
            CreatedUserName = "Created_Center",
            CreatedTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ModifiedUserId = 2002,
            ModifiedUserName = "Modified_Center"
        };
        expected.ModifiedTime = expected.CreatedTime.AddTicks(5);

        var json = JsonConvert.SerializeObject(expected, settings);
        var actual = JsonConvert.DeserializeObject<V0FourierCenterChannelFlexibleApertureCache>(json, settings);
        actual.Should().NotBeNull();
        actual.HasErrors.Should().BeFalse();

        actual.XMinMotorAbsoluteValue.Should().Be(expected.XMinMotorAbsoluteValue);
        actual.XMaxMotorAbsoluteValue.Should().Be(expected.XMaxMotorAbsoluteValue);
        actual.PushMinMotorAbsoluteValue.Should().Be(expected.PushMinMotorAbsoluteValue);
        actual.PushMaxMotorAbsoluteValue.Should().Be(expected.PushMaxMotorAbsoluteValue);
        actual.YMinMotorAbsoluteValue.Should().Be(expected.YMinMotorAbsoluteValue);
        actual.YMaxMotorAbsoluteValue.Should().Be(expected.YMaxMotorAbsoluteValue);
        actual.ProductivityInformation.Should().Be(expected.ProductivityInformation);
        actual.MicroscopeLensInformation.Should().Be(expected.MicroscopeLensInformation);
        actual.LaserLightInformation.Should().Be(expected.LaserLightInformation);
        actual.OpticsConfiguration.OpticsApodizationModeEnum.Should().Be(expected.OpticsConfiguration.OpticsApodizationModeEnum);
        actual.OpticsConfiguration.OpticsPolarizationModeEnum.Should().Be(expected.OpticsConfiguration.OpticsPolarizationModeEnum);
        actual.OpticsConfiguration.OpticsCollectorPolarizationModeEnum.Should().Be(expected.OpticsConfiguration.OpticsCollectorPolarizationModeEnum);
        actual.ScanLength.Should().Be(expected.ScanLength);
        actual.HazeFindBFMachinePosition.Should().Be(expected.HazeFindBFMachinePosition);
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

        JsonConvert.SerializeObject(actual, settings).Should().Be(json);
    }

    private class PrivateSetterContractResolver : DefaultContractResolver
    {
        public static readonly JsonSerializerSettings Settings = new()
        {
            ContractResolver = new PrivateSetterContractResolver()
        };

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (property.Writable || member is not PropertyInfo propertyInfo) return property;

            var hasPrivateSetter = propertyInfo.GetSetMethod(true) is not null;
            property.Writable = hasPrivateSetter;

            return property;
        }
    }
}