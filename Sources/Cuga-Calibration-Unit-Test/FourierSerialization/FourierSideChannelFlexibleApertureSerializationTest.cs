using AwesomeAssertions;
using AwesomeAssertions.Execution;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Fourier.SideChannelFlexibleAperture;
using Local.SQL.Cache.Providers.Serializations;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Models.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace CugaCalibrationUnitTest.FourierSerialization;

public sealed class FourierSideChannelFlexibleApertureSerializationTest(HostFixture fixture) : IClassFixture<HostFixture>
{
    private static readonly string[] NonPersistentNames =
    [
        "Document",
        "BitmapImageROIDrawable",
        "_resultBitmapImageDrawable",
        "_step0BitmapImageDrawable",
        "_step1BitmapImageDrawable",
        "_step2BitmapImageDrawable"
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
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "FourierSerialization", "FourierSideChannelFlexibleAperture.json"));
        var expected = JObject.Parse(json);

#pragma warning disable IDE0079
#pragma warning disable IDISP004

        using var actual = Guard.IsNotNullAndReturn(JsonConvert.DeserializeObject<FourierSideChannelFlexibleApertureDTO>(json, settings));

#pragma warning restore IDISP004
#pragma warning restore IDE0079

        actual.HasErrors.Should().BeFalse();

        AssertMatchesJson();

        var saved = JObject.Parse(JsonConvert.SerializeObject(actual, settings));
        saved.Descendants().OfType<JProperty>().Select(t => t.Name).Should().NotContain(NonPersistentNames);

        if (useCacheSettings == false)
        {
            RemoveMetadata(saved);
            saved.Remove(nameof(ICacheItem.IsDeleted)); // Rod / RodResult 的 IsDeleted 不能删除
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

        static void AssertChannelMatchesJson(FourierSideChannelFlexibleApertureDTOItem actual, JObject expected)
        {
            using var scope = new AssertionScope(expected.Path);

            ObjectHelper.GetFieldValue(actual, "_rodTotalCount").Should().Be(expected.Value<int>("_rodTotalCount"));
            actual.ChannelId.Should().Be(expected.Value<int>(nameof(actual.ChannelId)));
            actual.ChannelImageFilePath.Should().Be(expected.Value<string>(nameof(actual.ChannelImageFilePath)));
            actual.MinMotorAbsoluteValue.Should().Be(expected.Value<double>(nameof(actual.MinMotorAbsoluteValue)));
            actual.MaxMotorAbsoluteValue.Should().Be(expected.Value<double>(nameof(actual.MaxMotorAbsoluteValue)));

            AssertItemMatchesJson(actual.EvenItem, Guard.IsNotNullAndAssignableToTypeAndReturn<JObject>(expected[nameof(actual.EvenItem)]));
            AssertItemMatchesJson(actual.OddItem, Guard.IsNotNullAndAssignableToTypeAndReturn<JObject>(expected[nameof(actual.OddItem)]));
            AssertRodResultsMatchJson(actual.RodResults, Guard.IsNotNullAndAssignableToTypeAndReturn<JArray>(expected[nameof(actual.RodResults)]));
        }

        static void AssertItemMatchesJson(FourierSideChannelFlexibleApertureDTOItem.Item actual, JObject expected)
        {
            using var scope = new AssertionScope(expected.Path);

            ObjectHelper.GetFieldValue(actual, "_rodTotalCount").Should().Be(expected.Value<int>("_rodTotalCount"));
            ObjectHelper.GetFieldValue(actual, "_isEven").Should().Be(expected.Value<bool>("_isEven"));
            actual.Step0AndStep1MotorAbsoluteValue.Should().Be(expected.Value<double>(nameof(actual.Step0AndStep1MotorAbsoluteValue)));
            actual.Step2MotorAbsoluteValue.Should().Be(expected.Value<double>(nameof(actual.Step2MotorAbsoluteValue)));
            actual.Step0ChannelImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step0ChannelImageFilePath)));
            actual.Step1ChannelImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step1ChannelImageFilePath)));
            actual.Step2ChannelImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step2ChannelImageFilePath)));

            AssertRodMatchesJson(actual.Step0LeftRod, Guard.IsNotNullAndAssignableToTypeAndReturn<JToken>(expected[nameof(actual.Step0LeftRod)]));
            AssertRodMatchesJson(actual.Step0RightRod, Guard.IsNotNullAndAssignableToTypeAndReturn<JToken>(expected[nameof(actual.Step0RightRod)]));
            AssertRodsMatchJson(actual.Step1Rods, Guard.IsNotNullAndAssignableToTypeAndReturn<JArray>(expected[nameof(actual.Step1Rods)]));
            AssertRodsMatchJson(actual.Step2Rods, Guard.IsNotNullAndAssignableToTypeAndReturn<JArray>(expected[nameof(actual.Step2Rods)]));
        }

        static void AssertRodsMatchJson(FourierSideChannelFlexibleApertureDTOItem.Rod[] actual, JArray expected)
        {
            actual.Select(t => t.Index).Should().BeEquivalentTo(expected.Select(t => t.Value<int>("Index")), "{0} rod indexes", expected.Path);

            foreach (var token in expected) AssertRodMatchesJson(actual.Single(t => t.Index == token.Value<int>("Index")), token);
        }

        static void AssertRodResultsMatchJson(FourierSideChannelFlexibleApertureDTOItem.RodResult[] actual, JArray expected)
        {
            actual.Select(t => t.Index).Should().BeEquivalentTo(expected.Select(t => t.Value<int>("Index")), "{0} rod result indexes", expected.Path);

            foreach (var token in expected)
            {
                var rod = actual.Single(t => t.Index == token.Value<int>("Index"));

                using var rodScope = new AssertionScope(token.Path);

                rod.Index.Should().Be(token.Value<int>(nameof(rod.Index)));
                rod.IsDeleted.Should().Be(token.Value<bool>(nameof(rod.IsDeleted)));
                rod.PixelSize.Should().Be(token.Value<double>(nameof(rod.PixelSize)));

                AssertRectMatchesJson(rod.MinImageROI, Guard.IsNotNullAndAssignableToTypeAndReturn<JToken>(token[nameof(rod.MinImageROI)]));
                AssertRectMatchesJson(rod.MaxImageROI, Guard.IsNotNullAndAssignableToTypeAndReturn<JToken>(token[nameof(rod.MaxImageROI)]));
            }
        }

        static void AssertRodMatchesJson(FourierSideChannelFlexibleApertureDTOItem.Rod actual, JToken expected)
        {
            using var scope = new AssertionScope(expected.Path);

            actual.Index.Should().Be(expected.Value<int>(nameof(actual.Index)));
            actual.IsDeleted.Should().Be(expected.Value<bool>(nameof(actual.IsDeleted)));

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

        using var testScope = new AssertionScope($"seed={seed}, cache={useCacheSettings}");

        using var expected = CreateRandomDto(new Random(seed), rodTotalCount);

        var json = JsonConvert.SerializeObject(expected, settings);

#pragma warning disable IDE0079
#pragma warning disable IDISP004

        using var actual = Guard.IsNotNullAndReturn(JsonConvert.DeserializeObject<FourierSideChannelFlexibleApertureDTO>(json, settings));

#pragma warning restore IDISP004
#pragma warning restore IDE0079

        AssertEquals();

        var expectedJson = JObject.Parse(json);
        var saved = JObject.Parse(JsonConvert.SerializeObject(actual, settings));
        saved.Descendants().OfType<JProperty>().Select(t => t.Name).Should().NotContain(NonPersistentNames);
        saved.ToString(Formatting.Indented).Should().Be(expectedJson.ToString(Formatting.Indented));

        foreach (var name in IgnoreProperties)
        {
            expectedJson.ContainsKey(name).Should().Be(useCacheSettings == false, "{0} persistence must follow the selected settings", name);
        }

        expectedJson.ContainsKey(nameof(ICacheItem.IsDeleted)).Should().Be(useCacheSettings == false, "{0} persistence must follow the selected settings", nameof(ICacheItem.IsDeleted));

        return;

        static FourierSideChannelFlexibleApertureDTO CreateRandomDto(Random random, int rodTotalCount)
        {
            var dto = new FourierSideChannelFlexibleApertureDTO(
                rodTotalCount,
                $@"C:\Rnd\{random.Next(1000, 9999)}\step_{random.Next(0, 99)}_{random.Next(0, 99)}.jpg",
                $@"C:\Rnd\{random.Next(1000, 9999)}\step_{random.Next(0, 99)}_{random.Next(0, 99)}.jpg")
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

            foreach (var channel in new[] { dto.Channel1Item, dto.Channel2Item })
            {
                channel.MinMotorAbsoluteValue = Math.Round((random.NextDouble() - 0.5d) * 4000d, 3);
                channel.MaxMotorAbsoluteValue = Math.Round((random.NextDouble() - 0.5d) * 4000d, 3);

                foreach (var item in new[] { channel.EvenItem, channel.OddItem })
                {
                    item.Step0AndStep1MotorAbsoluteValue = Math.Round((random.NextDouble() - 0.5d) * 4000d, 3);
                    item.Step2MotorAbsoluteValue = Math.Round((random.NextDouble() - 0.5d) * 4000d, 3);
                    item.Step0ChannelImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\step_{random.Next(0, 99)}_{random.Next(0, 99)}.jpg";
                    item.Step1ChannelImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\step_{random.Next(0, 99)}_{random.Next(0, 99)}.jpg";
                    item.Step2ChannelImageFilePath = $@"C:\Rnd\{random.Next(1000, 9999)}\step_{random.Next(0, 99)}_{random.Next(0, 99)}.jpg";

                    foreach (var rod in new[] { item.Step0LeftRod, item.Step0RightRod }.Concat(item.Step1Rods).Concat(item.Step2Rods))
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
                    }
                }

                foreach (var rod in channel.RodResults)
                {
                    rod.IsDeleted = random.Next(2) == 1;
                    rod.PixelSize = Math.Round((random.NextDouble() - 0.5d) * 4000d, 3);
                    rod.MinImageROI = random.Next(6) switch
                    {
                        0 => Rect.Empty,
                        1 => new Rect(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), 0d, 0d),
                        2 => new Rect(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), 0d, random.NextDouble() * 4000d + 1d),
                        3 => new Rect(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), random.NextDouble() * 4000d + 1d, 0d),
                        _ => new Rect(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Abs(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3)), Math.Abs(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3)))
                    };
                    rod.MaxImageROI = random.Next(6) switch
                    {
                        0 => Rect.Empty,
                        1 => new Rect(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), 0d, 0d),
                        2 => new Rect(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), 0d, random.NextDouble() * 4000d + 1d),
                        3 => new Rect(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), random.NextDouble() * 4000d + 1d, 0d),
                        _ => new Rect(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Round((random.NextDouble() - 0.5d) * 4000d, 3), Math.Abs(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3)), Math.Abs(Math.Round((random.NextDouble() - 0.5d) * 4000d, 3)))
                    };
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
            actual.Id.Should().Be(useCacheSettings ? 0L : expected.Id);
            actual.Expiration.Should().Be(useCacheSettings ? 0L : expected.Expiration);
            actual.IsDeleted.Should().Be(useCacheSettings == false && expected.IsDeleted);
            actual.CreatedUserId.Should().Be(useCacheSettings ? 0L : expected.CreatedUserId);
            actual.CreatedUserName.Should().Be(useCacheSettings ? string.Empty : expected.CreatedUserName);
            actual.CreatedTime.Should().Be(useCacheSettings ? default : expected.CreatedTime);
            actual.ModifiedUserId.Should().Be(useCacheSettings ? 0L : expected.ModifiedUserId);
            actual.ModifiedUserName.Should().Be(useCacheSettings ? string.Empty : expected.ModifiedUserName);
            actual.ModifiedTime.Should().Be(useCacheSettings ? default : expected.ModifiedTime);
            actual.HasErrors.Should().BeFalse();

            AssertChannelEquals(actual.Channel1Item, expected.Channel1Item);
            AssertChannelEquals(actual.Channel2Item, expected.Channel2Item);
        }

        static void AssertChannelEquals(FourierSideChannelFlexibleApertureDTOItem actual, FourierSideChannelFlexibleApertureDTOItem expected)
        {
            ObjectHelper.GetFieldValue(actual, "_rodTotalCount").Should().Be(ObjectHelper.GetFieldValue(expected, "_rodTotalCount"));
            actual.ChannelId.Should().Be(expected.ChannelId);
            actual.ChannelImageFilePath.Should().Be(expected.ChannelImageFilePath);
            actual.MinMotorAbsoluteValue.Should().Be(expected.MinMotorAbsoluteValue);
            actual.MaxMotorAbsoluteValue.Should().Be(expected.MaxMotorAbsoluteValue);

            AssertItemEquals(actual.EvenItem, expected.EvenItem);
            AssertItemEquals(actual.OddItem, expected.OddItem);
            actual.RodResults.Should().Equal(expected.RodResults, (t1, t2) => t1.Index == t2.Index);

            foreach (var source in expected.RodResults)
            {
                var rod = actual.RodResults.Single(t => t.Index == source.Index);

                rod.Index.Should().Be(source.Index);
                rod.IsDeleted.Should().Be(source.IsDeleted);
                rod.PixelSize.Should().Be(source.PixelSize);
                rod.MinImageROI.Should().Be(source.MinImageROI);
                rod.MaxImageROI.Should().Be(source.MaxImageROI);
            }
        }

        static void AssertItemEquals(FourierSideChannelFlexibleApertureDTOItem.Item actual, FourierSideChannelFlexibleApertureDTOItem.Item expected)
        {
            ObjectHelper.GetFieldValue(actual, "_rodTotalCount").Should().Be(ObjectHelper.GetFieldValue(expected, "_rodTotalCount"));
            ObjectHelper.GetFieldValue(actual, "_isEven").Should().Be(ObjectHelper.GetFieldValue(expected, "_isEven"));
            actual.Step0AndStep1MotorAbsoluteValue.Should().Be(expected.Step0AndStep1MotorAbsoluteValue);
            actual.Step2MotorAbsoluteValue.Should().Be(expected.Step2MotorAbsoluteValue);
            actual.Step0ChannelImageFilePath.Should().Be(expected.Step0ChannelImageFilePath);
            actual.Step1ChannelImageFilePath.Should().Be(expected.Step1ChannelImageFilePath);
            actual.Step2ChannelImageFilePath.Should().Be(expected.Step2ChannelImageFilePath);

            AssertRodEquals(actual.Step0LeftRod, expected.Step0LeftRod);
            AssertRodEquals(actual.Step0RightRod, expected.Step0RightRod);
            actual.Step1Rods.Should().Equal(expected.Step1Rods, (t1, t2) => t1.Index == t2.Index);
            actual.Step2Rods.Should().Equal(expected.Step2Rods, (t1, t2) => t1.Index == t2.Index);

            foreach (var source in expected.Step1Rods) AssertRodEquals(actual.Step1Rods.Single(t => t.Index == source.Index), source);
            foreach (var source in expected.Step2Rods) AssertRodEquals(actual.Step2Rods.Single(t => t.Index == source.Index), source);
        }

        static void AssertRodEquals(FourierSideChannelFlexibleApertureDTOItem.Rod actual, FourierSideChannelFlexibleApertureDTOItem.Rod expected)
        {
            actual.Index.Should().Be(expected.Index);
            actual.IsDeleted.Should().Be(expected.IsDeleted);
            actual.ImageROI.Should().Be(expected.ImageROI);
        }
    }

    [Fact]
    public void Cache_ShouldMatchEveryField_AfterJsonRoundTrip()
    {
        var cookie = fixture.Host.Services.GetRequiredService<ApplicationCookie>();
        var expected = new FourierSideChannelFlexibleApertureCache
        {
            RodTotalCount = 46,
            MinMotorAbsoluteValue = -12.5d,
            MaxMotorAbsoluteValue = 88.25d,
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
            Step0AndStep1MotorAbsoluteValue = 40.5d,
            Step2MotorAbsoluteValue = 30.75d,
            HazeFindBFMachinePosition = new Point(11.5, 21.5),
            AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum.Ncc,
            AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum.Size128,
            Id = 31,
            Expiration = 32,
            IsDeleted = true,
            CreatedUserId = 2001,
            CreatedUserName = "Created_Flexible",
            CreatedTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ModifiedUserId = 2002,
            ModifiedUserName = "Modified_Flexible"
        };
        expected.ModifiedTime = expected.CreatedTime.AddTicks(5);

        var json = JsonConvert.SerializeObject(expected);
        var actual = JsonConvert.DeserializeObject<FourierSideChannelFlexibleApertureCache>(json);

        actual.Should().NotBeNull();
        actual.RodTotalCount.Should().Be(expected.RodTotalCount);
        actual.MinMotorAbsoluteValue.Should().Be(expected.MinMotorAbsoluteValue);
        actual.MaxMotorAbsoluteValue.Should().Be(expected.MaxMotorAbsoluteValue);
        actual.ProductivityInformation.Should().Be(expected.ProductivityInformation);
        actual.MicroscopeLensInformation.Should().Be(expected.MicroscopeLensInformation);
        actual.LaserLightInformation.Should().Be(expected.LaserLightInformation);
        actual.OpticsConfiguration.OpticsApodizationModeEnum.Should().Be(expected.OpticsConfiguration.OpticsApodizationModeEnum);
        actual.OpticsConfiguration.OpticsPolarizationModeEnum.Should().Be(expected.OpticsConfiguration.OpticsPolarizationModeEnum);
        actual.OpticsConfiguration.OpticsCollectorPolarizationModeEnum.Should().Be(expected.OpticsConfiguration.OpticsCollectorPolarizationModeEnum);
        actual.ScanLength.Should().Be(expected.ScanLength);
        actual.Step0AndStep1MotorAbsoluteValue.Should().Be(expected.Step0AndStep1MotorAbsoluteValue);
        actual.Step2MotorAbsoluteValue.Should().Be(expected.Step2MotorAbsoluteValue);
        actual.HazeFindBFMachinePosition.Should().Be(expected.HazeFindBFMachinePosition);
        actual.AlgorithmTemplateTypeEnum.Should().Be(expected.AlgorithmTemplateTypeEnum);
        actual.AlgorithmTemplateSizeEnum.Should().Be(expected.AlgorithmTemplateSizeEnum);
        actual.Id.Should().Be(expected.Id);
        actual.Expiration.Should().Be(expected.Expiration);
        actual.IsDeleted.Should().Be(expected.IsDeleted);
        actual.CreatedUserId.Should().Be(expected.CreatedUserId);
        actual.CreatedUserName.Should().Be(expected.CreatedUserName);
        actual.CreatedTime.Should().Be(expected.CreatedTime);
        actual.ModifiedUserId.Should().Be(expected.ModifiedUserId);
        actual.ModifiedUserName.Should().Be(expected.ModifiedUserName);
        actual.ModifiedTime.Should().Be(expected.ModifiedTime);
        actual.HasErrors.Should().BeFalse();

        JsonConvert.SerializeObject(actual).Should().Be(json);
    }
}