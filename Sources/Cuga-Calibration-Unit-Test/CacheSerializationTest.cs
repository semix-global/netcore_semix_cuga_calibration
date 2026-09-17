using AwesomeAssertions;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.CIB.LineCentricity;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Microsoft.Extensions.DependencyInjection;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Models.Serializations;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using Point = Net.Utilities.Models.Geometries.Point;

namespace CugaCalibrationUnitTest;

public sealed class CacheSerializationTest(HostFixture fixture) : IClassFixture<HostFixture>
{
    private readonly ApplicationCookie _applicationCookie = fixture.Host.Services.GetRequiredService<ApplicationCookie>();

    [Fact]
    public void LaserLineCentricityCacheSerialization_ShouldBeConsistent()
    {
        var oiCacheItem = new CIBLineCentricityCacheItem
        {
            MicroscopeLensInformation = _applicationCookie.MicroscopeLensInformations[0],
            LaserLightInformation = _applicationCookie.LaserLightInformations[0],
            CIBInformation = _applicationCookie.CIBInformations[0],
            OpticsConfiguration = new OpticsConfiguration
            {
                OpticsApodizationModeEnum = OpticsApodizationModeEnum.Gaussian,
                OpticsPolarizationModeEnum = OpticsPolarizationModeEnum.S,
                OpticsCollectorPolarizationModeEnum = OpticsCollectorPolarizationModeEnum.P
            },
            CIBConfiguration = new CIBConfiguration
            {
                Gain = 3,
                IsAutoGainControl = false,
                IsL0K = true,
                CIBProfileMode = CIBProfileModeEnum.PMTVoltage,
                IsKeepRawImageCIBProfileModeEnum = false
            },
            FindBFMachinePosition = new Point(100.5, 200.5),
            ImageWidth = 1024,
            BrightTemplateFilePath = @"C:\Test\OI_bright.tpl",
            BrightTemplateImageFilePath = @"C:\Test\OI_bright.jpg",
            TemplateFilePath = @"C:\Test\OI_template.tpl",
            TemplateImageFilePath = @"C:\Test\OI_template.jpg",
            IsDarkFieldAlignment = false,
            AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum.Sharpe,
            AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum.Size128,
            Id = 11,
            Expiration = 22,
            IsDeleted = false,
            CreatedUserId = 11,
            CreatedUserName = "Created_OI",
            CreatedTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ModifiedUserId = 12,
            ModifiedUserName = "Modified_OI"
        };
        oiCacheItem.ModifiedTime = oiCacheItem.CreatedTime.AddTicks(1);

        var niCacheItem = new CIBLineCentricityCacheItem
        {
            MicroscopeLensInformation = _applicationCookie.MicroscopeLensInformations[^1],
            LaserLightInformation = _applicationCookie.LaserLightInformations[^1],
            CIBInformation = _applicationCookie.CIBInformations[^1],
            OpticsConfiguration = new OpticsConfiguration
            {
                OpticsApodizationModeEnum = OpticsApodizationModeEnum.SuperGaussian,
                OpticsPolarizationModeEnum = OpticsPolarizationModeEnum.C,
                OpticsCollectorPolarizationModeEnum = OpticsCollectorPolarizationModeEnum.None
            },
            CIBConfiguration = new CIBConfiguration
            {
                Gain = -5,
                IsAutoGainControl = true,
                IsL0K = false,
                CIBProfileMode = CIBProfileModeEnum.PMTLog,
                IsKeepRawImageCIBProfileModeEnum = true
            },
            FindBFMachinePosition = new Point(150.0, 250.0),
            ImageWidth = 2048,
            BrightTemplateFilePath = @"C:\Test\NI_bright.tpl",
            BrightTemplateImageFilePath = @"C:\Test\NI_bright.jpg",
            TemplateFilePath = @"C:\Test\NI_template.tpl",
            TemplateImageFilePath = @"C:\Test\NI_template.jpg",
            IsDarkFieldAlignment = true,
            AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum.Ncc,
            AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum.Size512,
            Id = 33,
            Expiration = 44,
            IsDeleted = true,
            CreatedUserId = 33,
            CreatedUserName = "Created_NI",
            CreatedTime = new DateTime(2001, 2, 2, 0, 0, 0, DateTimeKind.Utc),
            ModifiedUserId = 34,
            ModifiedUserName = "Modified_NI"
        };
        niCacheItem.ModifiedTime = niCacheItem.CreatedTime.AddTicks(2);

        var cache = new CIBLineCentricityCache
        {
            ProductivityInformation = _applicationCookie.OIProductivityInformations[0],
            CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel,
            PmtInterval = 640,
            Threshold = new Point(1.5, 2.5),
            AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum.Sharpe,
            AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum.Size64,
            Id = 101,
            Expiration = 202,
            IsDeleted = true,
            CreatedUserId = 101,
            CreatedUserName = "Created_LineCentricity",
            CreatedTime = new DateTime(2000, 3, 3, 0, 0, 0, DateTimeKind.Utc),
            ModifiedUserId = 102,
            ModifiedUserName = "Modified_LineCentricity",
            Items = new ConcurrentDictionary<ProductivityInformation, CIBLineCentricityCacheItem>
            {
                [_applicationCookie.OIProductivityInformations[0]] = oiCacheItem,
                [_applicationCookie.NIProductivityInformations[0]] = niCacheItem
            }
        };
        cache.ModifiedTime = cache.CreatedTime.AddTicks(3);

        ObjectHelper.SetPropertyValue(cache, nameof(cache.Items), new ConcurrentDictionary<ProductivityInformation, CIBLineCentricityCacheItem>(cache.Items.OrderBy(t => t.Key)));
        var json = JsonConvert.SerializeObject(cache);
        var deserialized = JsonConvert.DeserializeObject<CIBLineCentricityCache>(json);

        deserialized.Should().NotBeNull();
        ObjectHelper.SetPropertyValue(deserialized, nameof(deserialized.Items), new ConcurrentDictionary<ProductivityInformation, CIBLineCentricityCacheItem>(deserialized.Items.OrderBy(t => t.Key)));

        deserialized.ProductivityInformation.Should().Be(cache.ProductivityInformation);
        deserialized.CalChipSiteModelEnum.Should().Be(cache.CalChipSiteModelEnum);
        deserialized.PmtInterval.Should().Be(cache.PmtInterval);
        deserialized.Threshold.Should().Be(cache.Threshold);
        deserialized.AlgorithmTemplateTypeEnum.Should().Be(cache.AlgorithmTemplateTypeEnum);
        deserialized.AlgorithmTemplateSizeEnum.Should().Be(cache.AlgorithmTemplateSizeEnum);
        deserialized.Id.Should().Be(cache.Id);
        deserialized.Expiration.Should().Be(cache.Expiration);
        deserialized.IsDeleted.Should().Be(cache.IsDeleted);
        deserialized.CreatedUserId.Should().Be(cache.CreatedUserId);
        deserialized.CreatedUserName.Should().Be(cache.CreatedUserName);
        deserialized.CreatedTime.Should().Be(cache.CreatedTime);
        deserialized.ModifiedUserId.Should().Be(cache.ModifiedUserId);
        deserialized.ModifiedUserName.Should().Be(cache.ModifiedUserName);
        deserialized.ModifiedTime.Should().Be(cache.ModifiedTime);
        deserialized.HasErrors.Should().BeFalse();

        deserialized.Items.Should().HaveCount(2);
        AssertItemEquals(deserialized.Items.Single(i => i.Key == _applicationCookie.OIProductivityInformations[0]).Value, oiCacheItem);
        AssertItemEquals(deserialized.Items.Single(i => i.Key == _applicationCookie.NIProductivityInformations[0]).Value, niCacheItem);

        JsonConvert.SerializeObject(deserialized).Should().Be(json);

        return;

        static void AssertItemEquals(CIBLineCentricityCacheItem actual, CIBLineCentricityCacheItem expected)
        {
            actual.MicroscopeLensInformation.Should().Be(expected.MicroscopeLensInformation);
            actual.LaserLightInformation.Should().Be(expected.LaserLightInformation);
            actual.CIBInformation.Should().Be(expected.CIBInformation);
            actual.OpticsConfiguration.OpticsApodizationModeEnum.Should().Be(expected.OpticsConfiguration.OpticsApodizationModeEnum);
            actual.OpticsConfiguration.OpticsPolarizationModeEnum.Should().Be(expected.OpticsConfiguration.OpticsPolarizationModeEnum);
            actual.OpticsConfiguration.OpticsCollectorPolarizationModeEnum.Should().Be(expected.OpticsConfiguration.OpticsCollectorPolarizationModeEnum);
            actual.CIBConfiguration.Gain.Should().Be(expected.CIBConfiguration.Gain);
            actual.CIBConfiguration.IsAutoGainControl.Should().Be(expected.CIBConfiguration.IsAutoGainControl);
            actual.CIBConfiguration.IsL0K.Should().Be(expected.CIBConfiguration.IsL0K);
            actual.CIBConfiguration.CIBProfileMode.Should().Be(expected.CIBConfiguration.CIBProfileMode);
            actual.CIBConfiguration.IsKeepRawImageCIBProfileModeEnum.Should().Be(expected.CIBConfiguration.IsKeepRawImageCIBProfileModeEnum);
            actual.FindBFMachinePosition.Should().Be(expected.FindBFMachinePosition);
            actual.ImageWidth.Should().Be(expected.ImageWidth);
            actual.BrightTemplateFilePath.Should().Be(expected.BrightTemplateFilePath);
            actual.BrightTemplateImageFilePath.Should().Be(expected.BrightTemplateImageFilePath);
            actual.TemplateFilePath.Should().Be(expected.TemplateFilePath);
            actual.TemplateImageFilePath.Should().Be(expected.TemplateImageFilePath);
            actual.IsDarkFieldAlignment.Should().Be(expected.IsDarkFieldAlignment);
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
        }
    }

    [Fact]
    public void FooSerialization_ShouldOnlySerializePublicProperties_AndSupportPrivateSetters()
    {
        var foo = new Foo(
            22,
            33,
            44,
            55
        )
        {
            PublicPropertyPublicSetInt = 11
        };

        // Act
        var json = JsonConvert.SerializeObject(foo, PrivateSetterContractResolver.Settings);
        var deserialized = JsonConvert.DeserializeObject<Foo>(json, PrivateSetterContractResolver.Settings);

        // Assert
        json.Should().Contain(nameof(Foo.PublicPropertyPublicSetInt));
        json.Should().Contain(nameof(Foo.PublicPropertyPrivateSetInt));
        json.Should().Contain(nameof(Foo.PublicPropertyInternalSetInt));
        json.Should().Contain(nameof(Foo.PublicPropertyReadOnlyInt));

        json.Should().NotContain("PrivatePropertyPrivateSetInt");

        deserialized.Should().NotBeNull();
        deserialized.PublicPropertyPublicSetInt.Should().Be(11);
        deserialized.PublicPropertyPrivateSetInt.Should().Be(22);
        deserialized.PublicPropertyInternalSetInt.Should().Be(33);
        deserialized.PublicPropertyReadOnlyInt.Should().Be(4);
        ObjectHelper.GetPropertyValue(deserialized, "PrivatePropertyPrivateSetInt").Should().Be(5);
    }

    public sealed class Foo
    {
        public int PublicPropertyPublicSetInt { get; set; } = 1;

        public int PublicPropertyPrivateSetInt { get; private set; } = 2;

        public int PublicPropertyInternalSetInt { get; internal set; } = 3;

        public int PublicPropertyReadOnlyInt { get; } = 4;

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private int PrivatePropertyPrivateSetInt { get; set; } = 5;

        public Foo()
        {
        }

        public Foo(
            int publicPropertyPrivateSetInt,
            int publicPropertyInternalSetInt,
            int publicPropertyReadOnlyInt,
            int privatePropertyPrivateSetInt)
        {
            PublicPropertyPrivateSetInt = publicPropertyPrivateSetInt;
            PublicPropertyInternalSetInt = publicPropertyInternalSetInt;
            PublicPropertyReadOnlyInt = publicPropertyReadOnlyInt;
            PrivatePropertyPrivateSetInt = privatePropertyPrivateSetInt;
        }
    }
}