using AwesomeAssertions;
using Core.Models;
using Core.Models.Helper;
using Core.Models.Models.CIB.LineCentricity;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Recipe.Services;
using Core.Services;
using CugaCalibration.Core;
using CugaCalibration.ViewModels.Common;
using Local.SQL.Cache.Providers;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Local.SQL.DB.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Models;
using Net.Utilities.Models.Serializations;
using Net.Utilities.WPF.MVVM;
using Newtonsoft.Json;
using SourceGenerator.AssemblyMetadata;
using System.Collections.Concurrent;
using System.Windows;
using Point = Net.Utilities.Models.Geometries.Point;

namespace CugaCalibrationUnitTest;

/// <summary>
/// ValueTuple Key 序列化和反序列化一致性测试
/// </summary>
public sealed class CacheSerializationTest : IDisposable
{
    private static readonly Application Application = new();

    private readonly IHost _host;
    private readonly ProductivityInformation _oiProductivityInfo;
    private readonly ProductivityInformation _niProductivityInfo;

    public CacheSerializationTest()
    {
#pragma warning disable IDE0079
#pragma warning disable IDISP004

        _host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureServices((context, services) =>
            {
                services
                    .Configure<ApplicationSetting>(context.Configuration.GetSection(BaseApplicationSetting.AppSetting))
                    .AddMvvmService(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, CugaCalibrationUnitTestAssemblyMetadata.Version, Application, context.HostingEnvironment)
                    .AddSqlDbContext(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value.SqlDbDataSource, context.HostingEnvironment)
                    .AddCacheContext(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value.NosqlDbDataSource, sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, context.HostingEnvironment)
                    .AddRecipeService(context.HostingEnvironment)
                    .AddKeyedCacheContext(CalibrationConstantsHelper.RecipeDbKey, sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, context.HostingEnvironment)
                    .AddCoreService(context.HostingEnvironment)
                    .AddApplication(context.HostingEnvironment);
            })
            .UseEnvironment(Environments.Development)
            .Build()
            .ConfigureHostApplication();

#pragma warning restore IDISP004
#pragma warning restore IDE0079

        var microscopeLensInformations = HostApplication.GetRequiredService<MicroscopeViewModel>().GetMicroscopeLensInformations();
        var laserLightInformations = HostApplication.GetRequiredService<LaserViewModel>().GetLaserLightInformations();
        var productivityInformations = HostApplication.GetRequiredService<OpticsViewModel>().GetProductivityInformations();
        var cibInformations = HostApplication.GetRequiredService<CIBViewModel>().GetCIBInformations();

        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();
        applicationCookie.MicroscopeLensInformations = [.. microscopeLensInformations.Select(t => t.Clone())];
        applicationCookie.LaserLightInformations = [.. laserLightInformations.Select(t => t.Clone())];
        applicationCookie.ProductivityInformations = [.. productivityInformations.Select(t => t.Clone())];
        applicationCookie.CIBInformations = [.. cibInformations.Select(t => t.Clone())];

        _oiProductivityInfo = applicationCookie.OIProductivityInformations[0];
        _niProductivityInfo = applicationCookie.NIProductivityInformations[0];
    }

    public void Dispose()
    {
        HostApplication.GetRequiredService<IFreeSql>().Dispose();
        HostApplication.GetRequiredService<ICacheProvider>().Dispose();
        HostApplication.GetKeyedService<ICacheProvider>(CalibrationConstantsHelper.RecipeDbKey).Dispose();
        _host.Dispose();
    }

    [Fact]
    public void LaserLineCentricityCacheSerialization_ShouldBeConsistent()
    {
        // Arrange - 创建包含多个items的Cache
        var oiCacheItem = new CIBLineCentricityCacheItem
        {
            ImageWidth = 1024,
            FindBFMachinePosition = new Point(100.5, 200.5),
            BrightTemplateFilePath = @"C:\Test\OI_bright.tpl",
            TemplateFilePath = @"C:\Test\OI_template.tpl"
        };

        var niCacheItem = new CIBLineCentricityCacheItem
        {
            ImageWidth = 2048,
            FindBFMachinePosition = new Point(150.0, 250.0),
            BrightTemplateFilePath = @"C:\Test\NI_bright.tpl",
            TemplateFilePath = @"C:\Test\NI_template.tpl"
        };

        var cache = new CIBLineCentricityCache
        {
            Items = new ConcurrentDictionary<ProductivityInformation, CIBLineCentricityCacheItem>
            {
                [_oiProductivityInfo] = oiCacheItem,
                [_niProductivityInfo] = niCacheItem
            }
        };

        // Act - 序列化和反序列化
        ObjectHelper.SetPropertyValue(cache, nameof(cache.Items), new ConcurrentDictionary<ProductivityInformation, CIBLineCentricityCacheItem>(cache.Items.OrderBy(t => t.Key)));
        var json = JsonConvert.SerializeObject(cache);
        var deserialized = JsonConvert.DeserializeObject<CIBLineCentricityCache>(json);

        // Assert
        deserialized.Should().NotBeNull();
        ObjectHelper.SetPropertyValue(deserialized, nameof(deserialized.Items),
            new ConcurrentDictionary<ProductivityInformation, CIBLineCentricityCacheItem>(deserialized.Items.OrderBy(t => t.Key)));

        deserialized.Items.Should().NotBeNull();
        deserialized.Items.Should().HaveCount(2);

        // 验证OI item
        var oiItem = deserialized.Items.Single(i => i.Key == _oiProductivityInfo);
        oiItem.Value.ImageWidth.Should().Be(1024);
        oiItem.Value.FindBFMachinePosition.Should().Be(new Point(100.5, 200.5));

        // 验证NI item
        var niItem = deserialized.Items.Single(i => i.Key == _niProductivityInfo);
        niItem.Value.ImageWidth.Should().Be(2048);
        niItem.Value.FindBFMachinePosition.Should().Be(new Point(150.0, 250.0));

        JsonConvert.SerializeObject(deserialized).Should().Be(json);
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