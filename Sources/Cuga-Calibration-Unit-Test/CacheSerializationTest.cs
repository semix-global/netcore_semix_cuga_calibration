using AwesomeAssertions;
using Core.Models.Enums.Optics;
using Core.Models.Models.Chuck.AlignmentDegreeOffset;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Laser.LineCentricity;
using Core.Models.Models.Laser.PixelSize;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Windows;
using Core.Models.Helper;
using Core.Models.Models.Common.Cookies;
using Core.Services;
using Core.Utilities;
using CugaCalibration.Core;
using CugaCalibration.ViewModels.Common;
using Local.NoSQL.DB.Providers;
using Local.NoSQL.DB.Providers.Interfaces;
using Local.SQL.DB.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Models;
using Net.Utilities.ScottPlot.WPF;
using Net.Utilities.WPF.MVVM;
using SourceGenerator.AssemblyMetadata;
using SourceGenerator.InjectHostDI;
using Xunit;
using Point = Net.Utilities.Models.Geometries.Point;

namespace CugaCalibrationUnitTest;

/// <summary>
/// ValueTuple Key 序列化和反序列化一致性测试
/// </summary>
public sealed class CacheSerializationTest : IDisposable
{
    private static readonly Application Application = new();
    private readonly ProductivityInformation _oiProductivityInfo;
    private readonly ProductivityInformation _niProductivityInfo;

    public CacheSerializationTest()
    {
#pragma warning disable IDISP004

        Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureServices((context, services) =>
            {
                services
                    .Configure<ApplicationSetting>(context.Configuration.GetSection(BaseApplicationSetting.AppSetting))
                    .AddMvvmService(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, CugaCalibrationTestAssemblyMetadata.Version, Application, context.HostingEnvironment)
                    .AddSqlDbContext(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value.SqlDbDataSource, context.HostingEnvironment)
                    .AddNoSQLDBContext(sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value.NosqlDbDataSource, sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, context.HostingEnvironment)
                    .AddKeyedNoSQLDBContext(CalibrationConstantsHelper.RecipeDbKey, sp => sp.GetRequiredService<IOptions<ApplicationSetting>>().Value, context.HostingEnvironment)
                    .AddScottPlotServices()
                    .AddCoreService(context.HostingEnvironment)
                    .AddApplication(context.HostingEnvironment)
                    .AddCugaCalibrationTestInjectHostDI(context.HostingEnvironment);
            })
            .UseEnvironment(Environments.Development)
            .Build()
            .ConfigureHostApplication();

#pragma warning restore IDISP004

        var microscopeLensInformations = HostApplication.GetRequiredService<MicroscopeViewModel>().GetMicroscopeLensInformations();
        var laserLightInformations = HostApplication.GetRequiredService<LaserViewModel>().GetLaserLightInformations();
        var productivityInformations = HostApplication.GetRequiredService<OpticsViewModel>().GetProductivityInformations();
        var cibInformations = HostApplication.GetRequiredService<CIBViewModel>().GetCIBInformations();

        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();
        applicationCookie.MicroscopeLensInformations = [.. microscopeLensInformations.Select(t => t.Clone())];
        applicationCookie.LaserLightInformations = [.. laserLightInformations.Select(t => t.Clone())];
        applicationCookie.ProductivityInformations = [.. productivityInformations.Select(t => t.Clone())];
        applicationCookie.CIBInformations = [.. cibInformations.Select(t => t.Clone())];

        _oiProductivityInfo = applicationCookie.OIProductivityInformations.FirstOrDefault()
                              ?? throw new InvalidOperationException("No OI ProductivityInformation available");
        _niProductivityInfo = applicationCookie.NIProductivityInformations.FirstOrDefault()
                              ?? throw new InvalidOperationException("No NI ProductivityInformation available");
    }

    public void Dispose()
    {
        HostApplication.GetRequiredService<IFreeSql>().Dispose();
        HostApplication.GetRequiredService<ICacheProvider>().Dispose();
        HostApplication.GetKeyedService<ICacheProvider>(CalibrationConstantsHelper.RecipeDbKey).Dispose();
    }

    [Fact]
    public void LaserLineCentricityCacheSerialization_ShouldBeConsistent()
    {
        // Arrange - 创建包含多个items的Cache
        var oiCacheItem = new LaserLineCentricityCacheItem
        {
            XWidthPixel = 1024,
            FindPosition = new Point(100.5, 200.5),
            FindBrightMachinePosition = new Point(300.0, 400.0),
            Threshold = new Point(0.8, 0.9),
            BrightTemplateFilePath = "C:\\Test\\OI_bright.tpl",
            TemplateFilePath = "C:\\Test\\OI_template.tpl"
        };

        var niCacheItem = new LaserLineCentricityCacheItem
        {
            XWidthPixel = 2048,
            FindPosition = new Point(150.0, 250.0),
            FindBrightMachinePosition = new Point(350.0, 450.0),
            Threshold = new Point(0.7, 0.85),
            BrightTemplateFilePath = "C:\\Test\\NI_bright.tpl",
            TemplateFilePath = "C:\\Test\\NI_template.tpl"
        };

        var cache = new LaserLineCentricityCache
        {
            Items = new ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), LaserLineCentricityCacheItem>>
            {
                new((OpticsIlluminationModeEnum.OI, _oiProductivityInfo), oiCacheItem),
                new((OpticsIlluminationModeEnum.NI, _niProductivityInfo), niCacheItem)
            }
        };

        // Act - 序列化和反序列化
        var json = JsonConvert.SerializeObject(cache.Items);
        var deserialized = JsonConvert.DeserializeObject<ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), LaserLineCentricityCacheItem>>>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Should().HaveCount(2);

        // 验证OI item
        var oiItem = deserialized!.Single(i => i.Key.Item1 == OpticsIlluminationModeEnum.OI);
        oiItem.Key.Item2.Should().Be(_oiProductivityInfo);
        oiItem.Value.XWidthPixel.Should().Be(1024);
        oiItem.Value.FindPosition.Should().Be(new Point(100.5, 200.5));
        oiItem.Value.Threshold.Should().Be(new Point(0.8, 0.9));

        // 验证NI item
        var niItem = deserialized!.Single(i => i.Key.Item1 == OpticsIlluminationModeEnum.NI);
        niItem.Key.Item2.Should().Be(_niProductivityInfo);
        niItem.Value.XWidthPixel.Should().Be(2048);
        niItem.Value.FindPosition.Should().Be(new Point(150.0, 250.0));
        niItem.Value.Threshold.Should().Be(new Point(0.7, 0.85));
    }

    [Fact]
    public void LaserPixelSizeCacheSerialization_ShouldBeConsistent()
    {
        // Arrange - 创建包含多个items的Cache
        var oiCacheItem = new LaserPixelSizeCacheItem
        {
            XWidthPixel = 2048,
            FindPosition = new Point(150.0, 250.0),
            VerifyResultYPixelSize = 1.25,
            Threshold = 0.95
        };

        var niCacheItem = new LaserPixelSizeCacheItem
        {
            XWidthPixel = 1024,
            FindPosition = new Point(200.0, 300.0),
            VerifyResultYPixelSize = 1.50,
            Threshold = 0.90
        };

        var cache = new LaserPixelSizeCache
        {
            Items = new ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), LaserPixelSizeCacheItem>>
            {
                new((OpticsIlluminationModeEnum.OI, _oiProductivityInfo), oiCacheItem),
                new((OpticsIlluminationModeEnum.NI, _niProductivityInfo), niCacheItem)
            }
        };

        // Act
        var json = JsonConvert.SerializeObject(cache.Items);
        var deserialized = JsonConvert.DeserializeObject<ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), LaserPixelSizeCacheItem>>>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Should().HaveCount(2);

        // 验证OI item
        var oiItem = deserialized!.Single(i => i.Key.Item1 == OpticsIlluminationModeEnum.OI);
        oiItem.Key.Item2.Should().Be(_oiProductivityInfo);
        oiItem.Value.XWidthPixel.Should().Be(2048);
        oiItem.Value.VerifyResultYPixelSize.Should().Be(1.25);
        oiItem.Value.Threshold.Should().Be(0.95);

        // 验证NI item
        var niItem = deserialized!.Single(i => i.Key.Item1 == OpticsIlluminationModeEnum.NI);
        niItem.Key.Item2.Should().Be(_niProductivityInfo);
        niItem.Value.XWidthPixel.Should().Be(1024);
        niItem.Value.VerifyResultYPixelSize.Should().Be(1.50);
        niItem.Value.Threshold.Should().Be(0.90);
    }

    [Fact]
    public void ChuckAlignmentDegreeOffsetCacheSerialization_ShouldBeConsistent()
    {
        // Arrange - 创建包含多个items的Cache
        var oiCacheItem = new ChuckAlignmentDegreeOffsetCacheItem { XWidthPixel = 512 };
        var niCacheItem = new ChuckAlignmentDegreeOffsetCacheItem { XWidthPixel = 1024 };

        var cache = new ChuckAlignmentDegreeOffsetCache
        {
            Items = new ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), ChuckAlignmentDegreeOffsetCacheItem>>
            {
                new((OpticsIlluminationModeEnum.OI, _oiProductivityInfo), oiCacheItem),
                new((OpticsIlluminationModeEnum.NI, _niProductivityInfo), niCacheItem)
            }
        };

        // Act
        var json = JsonConvert.SerializeObject(cache.Items);
        var deserialized = JsonConvert.DeserializeObject<ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), ChuckAlignmentDegreeOffsetCacheItem>>>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Should().HaveCount(2);

        // 验证OI item
        var oiItem = deserialized!.Single(i => i.Key.Item1 == OpticsIlluminationModeEnum.OI);
        oiItem.Key.Item2.Should().Be(_oiProductivityInfo);
        oiItem.Value.XWidthPixel.Should().Be(512);

        // 验证NI item
        var niItem = deserialized!.Single(i => i.Key.Item1 == OpticsIlluminationModeEnum.NI);
        niItem.Key.Item2.Should().Be(_niProductivityInfo);
        niItem.Value.XWidthPixel.Should().Be(1024);
    }
}