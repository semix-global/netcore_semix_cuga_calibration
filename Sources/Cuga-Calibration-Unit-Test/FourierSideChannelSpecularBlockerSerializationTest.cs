using AwesomeAssertions;
using AwesomeAssertions.Execution;
using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Fourier.SideChannelSpecularBlocker;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Reflection;

namespace CugaCalibrationUnitTest;

public sealed class FourierSideChannelSpecularBlockerSerializationTest
{
    private static readonly string[] NonPersistentNames = ["Document", "PMTDocument", "BitmapImageROIDrawable", "document", "pmtDocument", "bitmapImageROIDrawable"];
    private static readonly string[] InfrastructureNames =
    [
        "Id",
        "Expiration",
        "CreatedUserId",
        "CreatedUserName",
        "CreatedTime",
        "ModifiedUserId",
        "ModifiedUserName",
        "ModifiedTime",
        "HasErrors"
    ];

    private static JObject LoadProvidedJson() => JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "FourierSideChannelSpecularBlocker", "Json_1.json")));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProvidedJson_ShouldMatchEveryField_AndSerializeBackIdentically(bool useCacheSettings)
    {
        var settings = useCacheSettings ? PrivateSetterContractResolver.Settings : new JsonSerializerSettings();
        var provided = LoadProvidedJson();

        using var restored = Deserialize(provided.ToString(), settings);
        AssertDtoMatchesJson(provided, restored);
        AssertDrawableBindings(restored);

        var saved = JObject.Parse(JsonConvert.SerializeObject(restored, settings));
        saved.Descendants().OfType<JProperty>().Select(t => t.Name).Should().NotContain(NonPersistentNames);
        AssertJsonIdentical(provided, saved);
    }

    [Theory]
    [InlineData(6, 11, false)]
    [InlineData(8, 22, true)]
    [InlineData(9, 33, false)]
    [InlineData(23, 44, true)]
    [InlineData(46, 55, false)]
    public void RandomDto_ShouldMatchEveryField_AfterJsonRoundTrip(int rodTotalCount, int seed, bool useCacheSettings)
    {
        var settings = useCacheSettings ? PrivateSetterContractResolver.Settings : new JsonSerializerSettings();
        using var source = CreateRandomDto(new Random(seed), rodTotalCount);
        var json = JsonConvert.SerializeObject(source, settings);

        using var restored = Deserialize(json, settings);
        AssertDtoEquals(source, restored);
        AssertDrawableBindings(restored);
    }

    [Fact]
    public void RandomDto_ShouldMatchEveryField_AfterJsonRoundTrip_WithNewSeed()
    {
        var seed = new Random().Next();
        using var scope = new AssertionScope($"seed={seed}");
        RandomDto_ShouldMatchEveryField_AfterJsonRoundTrip(new Random(seed).Next(6, 47), seed, false);
    }

    private static FourierSideChannelSpecularBlockerDTO CreateRandomDto(Random random, int rodTotalCount)
    {
        var dto = new FourierSideChannelSpecularBlockerDTO(rodTotalCount)
        {
            IsCalibrated = NextBool(random),
            IsVerified = NextBool(random),
            IsRequiredSelfCheck = NextBool(random),
            Id = NextInt64(random),
            Expiration = NextInt64(random)
        };

        foreach (var channel in new[] { dto.Channel1Item, dto.Channel2Item })
        {
            channel.Step0FourierImageFilePath = RandomPath(random, "Step0Fourier");
            channel.Step1FourierImageFilePath = RandomPath(random, "Step1Fourier");
            channel.RawStep0PMTImageFilePath = RandomPath(random, "RawStep0PMT");
            channel.RawStep1PMTImageFilePath = RandomPath(random, "RawStep1PMT");
            channel.Step0PMTImageFilePath = RandomPath(random, "Step0PMT");
            channel.Step1PMTImageFilePath = RandomPath(random, "Step1PMT");
            channel.Step0PMTImageAverageValue = NextCoordinate(random);
            channel.Step1PMTImageAverageValue = NextCoordinate(random);
            channel.ExtinctionRatio = NextCoordinate(random);
            foreach (var rod in channel.Rods)
            {
                rod.IsDeleted = NextBool(random);
                rod.ImageROI = RandomRect(random);
                rod.MotorAbsoluteValue = NextCoordinate(random);
            }
        }

        return dto;
    }

    private static void AssertDtoMatchesJson(JObject expected, FourierSideChannelSpecularBlockerDTO actual)
    {
        using var scope = new AssertionScope("DTO");
        AssertPrivateFieldEqualsJson(expected, actual, "_rodTotalCount");
        AssertProductivityInformationMatchesJson(RequiredJson<JObject>(expected[nameof(actual.ProductivityInformation)]), actual.ProductivityInformation);
        actual.IsCalibrated.Should().Be(expected.Value<bool>(nameof(actual.IsCalibrated)));
        actual.IsVerified.Should().Be(expected.Value<bool>(nameof(actual.IsVerified)));
        actual.IsRequiredSelfCheck.Should().Be(expected.Value<bool>(nameof(actual.IsRequiredSelfCheck)));
        actual.IsOk.Should().Be(expected.Value<bool>(nameof(actual.IsOk)));
        if (expected[nameof(actual.Id)] is not null) actual.Id.Should().Be(expected.Value<long>(nameof(actual.Id)));
        if (expected[nameof(actual.Expiration)] is not null) actual.Expiration.Should().Be(expected.Value<long>(nameof(actual.Expiration)));
        AssertChannelMatchesJson(RequiredJson<JObject>(expected[nameof(actual.Channel1Item)]), actual.Channel1Item);
        AssertChannelMatchesJson(RequiredJson<JObject>(expected[nameof(actual.Channel2Item)]), actual.Channel2Item);
    }

    private static void AssertChannelMatchesJson(JObject expected, FourierSideChannelSpecularBlockerDTOItem actual)
    {
        using var scope = new AssertionScope(expected.Path);
        AssertPrivateFieldEqualsJson(expected, actual, "_rodTotalCount");
        actual.ChannelId.Should().Be(expected.Value<int>(nameof(actual.ChannelId)));
        actual.Step0FourierImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step0FourierImageFilePath)));
        actual.Step1FourierImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step1FourierImageFilePath)));
        actual.RawStep0PMTImageFilePath.Should().Be(expected.Value<string>(nameof(actual.RawStep0PMTImageFilePath)));
        actual.RawStep1PMTImageFilePath.Should().Be(expected.Value<string>(nameof(actual.RawStep1PMTImageFilePath)));
        actual.Step0PMTImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step0PMTImageFilePath)));
        actual.Step1PMTImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step1PMTImageFilePath)));
        actual.Step0PMTImageAverageValue.Should().Be(expected.Value<double>(nameof(actual.Step0PMTImageAverageValue)));
        actual.Step1PMTImageAverageValue.Should().Be(expected.Value<double>(nameof(actual.Step1PMTImageAverageValue)));
        actual.ExtinctionRatio.Should().Be(expected.Value<double>(nameof(actual.ExtinctionRatio)));
        AssertRodsMatchJson(RequiredJson<JArray>(expected[nameof(actual.Rods)]), actual.Rods);
    }

    private static void AssertRodsMatchJson(JArray expected, FourierSideChannelSpecularBlockerDTOItem.Rod[] actual)
    {
        actual.Select(t => t.Index).Should().BeEquivalentTo(expected.Select(t => t.Value<int>("Index")), "{0} rod indexes", expected.Path);
        foreach (var token in expected)
            AssertRodMatchesJson(token, actual.Single(t => t.Index == token.Value<int>("Index")));
    }

    private static void AssertRodMatchesJson(JToken expected, FourierSideChannelSpecularBlockerDTOItem.Rod actual)
    {
        using var scope = new AssertionScope(expected.Path);
        actual.Index.Should().Be(expected.Value<int>(nameof(actual.Index)));
        actual.IsDeleted.Should().Be(expected.Value<bool>(nameof(actual.IsDeleted)));
        actual.MotorAbsoluteValue.Should().Be(expected.Value<double>(nameof(actual.MotorAbsoluteValue)));
        AssertRectMatchesJson(RequiredJson<JToken>(expected[nameof(actual.ImageROI)]), actual.ImageROI);
    }

    private static void AssertRectMatchesJson(JToken expected, Rect actual)
    {
        using var scope = new AssertionScope(expected.Path);
        actual.X.Should().Be(expected.Value<double>(nameof(actual.X)));
        actual.Y.Should().Be(expected.Value<double>(nameof(actual.Y)));
        actual.Width.Should().Be(expected.Value<double>(nameof(actual.Width)));
        actual.Height.Should().Be(expected.Value<double>(nameof(actual.Height)));
    }

    private static void AssertProductivityInformationMatchesJson(JObject expected, ProductivityInformation actual)
    {
        using var scope = new AssertionScope(expected.Path);
        actual.OpticsIlluminationModeEnum.Should().Be((OpticsIlluminationModeEnum)expected.Value<int>(nameof(actual.OpticsIlluminationModeEnum)));
        actual.OpticsMagType.Should().Be(expected.Value<int>(nameof(actual.OpticsMagType)));
        actual.StageSpeedType.Should().Be(expected.Value<int>(nameof(actual.StageSpeedType)));
    }

    private static void AssertDtoEquals(FourierSideChannelSpecularBlockerDTO expected, FourierSideChannelSpecularBlockerDTO actual)
    {
        using var scope = new AssertionScope("DTO");
        GetPrivateField(actual, "_rodTotalCount").Should().Be(GetPrivateField(expected, "_rodTotalCount"));
        actual.ProductivityInformation.Should().Be(expected.ProductivityInformation);
        actual.IsCalibrated.Should().Be(expected.IsCalibrated);
        actual.IsVerified.Should().Be(expected.IsVerified);
        actual.IsRequiredSelfCheck.Should().Be(expected.IsRequiredSelfCheck);
        actual.IsOk.Should().Be(expected.IsOk);
        actual.Id.Should().Be(expected.Id);
        actual.Expiration.Should().Be(expected.Expiration);
        AssertChannelEquals(expected.Channel1Item, actual.Channel1Item);
        AssertChannelEquals(expected.Channel2Item, actual.Channel2Item);
    }

    private static void AssertChannelEquals(FourierSideChannelSpecularBlockerDTOItem expected, FourierSideChannelSpecularBlockerDTOItem actual)
    {
        GetPrivateField(actual, "_rodTotalCount").Should().Be(GetPrivateField(expected, "_rodTotalCount"));
        actual.ChannelId.Should().Be(expected.ChannelId);
        actual.Step0FourierImageFilePath.Should().Be(expected.Step0FourierImageFilePath);
        actual.Step1FourierImageFilePath.Should().Be(expected.Step1FourierImageFilePath);
        actual.RawStep0PMTImageFilePath.Should().Be(expected.RawStep0PMTImageFilePath);
        actual.RawStep1PMTImageFilePath.Should().Be(expected.RawStep1PMTImageFilePath);
        actual.Step0PMTImageFilePath.Should().Be(expected.Step0PMTImageFilePath);
        actual.Step1PMTImageFilePath.Should().Be(expected.Step1PMTImageFilePath);
        actual.Step0PMTImageAverageValue.Should().Be(expected.Step0PMTImageAverageValue);
        actual.Step1PMTImageAverageValue.Should().Be(expected.Step1PMTImageAverageValue);
        actual.ExtinctionRatio.Should().Be(expected.ExtinctionRatio);
        actual.Rods.Select(t => t.Index).Should().Equal(expected.Rods.Select(t => t.Index));
        foreach (var source in expected.Rods)
        {
            var rod = actual.Rods.Single(t => t.Index == source.Index);
            rod.IsDeleted.Should().Be(source.IsDeleted);
            rod.ImageROI.Should().Be(source.ImageROI);
            rod.MotorAbsoluteValue.Should().Be(source.MotorAbsoluteValue);
        }
    }

    private static void AssertJsonIdentical(JObject expected, JObject actual)
    {
        var expectedPayload = WithoutInfrastructure(expected);
        var actualPayload = WithoutInfrastructure(actual);
        AssertJsonSubtree(expectedPayload, actualPayload);
        AssertJsonSubtree(actualPayload, expectedPayload);
    }

    private static void AssertJsonSubtree(JToken expected, JToken actual)
    {
        using var scope = new AssertionScope(expected.Path);
        if (expected is JObject expectedObject)
        {
            var actualObject = RequiredJson<JObject>(actual);
            foreach (var property in expectedObject.Properties())
                AssertJsonSubtree(property.Value, RequiredJson<JToken>(actualObject.GetValue(property.Name, StringComparison.OrdinalIgnoreCase)));
        }
        else if (expected is JArray expectedArray)
        {
            var savedArray = RequiredJson<JArray>(actual);
            savedArray.Count.Should().Be(expectedArray.Count, "{0} length", expected.Path);
            foreach (var (left, right) in expectedArray.Zip(savedArray)) AssertJsonSubtree(left, right);
        }
        else
        {
            AssertJsonValueEquals(RequiredJson<JValue>(expected), RequiredJson<JValue>(actual), expected.Path);
        }
    }

    private static void AssertJsonValueEquals(JValue expected, JValue actual, string path)
    {
        if (IsNumber(expected) && IsNumber(actual))
        {
            Convert.ToDouble(actual.Value).Should().Be(Convert.ToDouble(expected.Value), path);
            return;
        }

        actual.Value.Should().Be(expected.Value, path);
    }

    private static JObject WithoutInfrastructure(JObject token)
    {
        var copy = RequiredJson<JObject>(token.DeepClone());
        copy.Remove("IsDeleted");
        foreach (var obj in copy.DescendantsAndSelf().OfType<JObject>())
            foreach (var name in InfrastructureNames)
                obj.Remove(name);

        return copy;
    }

    private static void AssertPrivateFieldEqualsJson(JObject expected, object actual, string name)
    {
        GetPrivateField(actual, name).Should().Be(RequiredJson<JToken>(expected[name]).ToObject(GetPrivateFieldInfo(actual, name).FieldType), "{0}.{1} must match JSON", expected.Path, name);
    }

    private static T RequiredJson<T>(JToken? token) where T : JToken =>
        Guard.IsAssignableToTypeAndReturn<T>(Guard.IsNotNullAndReturn(token));

    private static object? GetPrivateField(object actual, string name) => GetPrivateFieldInfo(actual, name).GetValue(actual);

    private static FieldInfo GetPrivateFieldInfo(object actual, string name)
    {
        return Guard.IsNotNullAndReturn(actual.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic));
    }

    private static void AssertDrawableBindings(FourierSideChannelSpecularBlockerDTO dto)
    {
        AssertChannelDrawableBindings(dto.Channel1Item);
        AssertChannelDrawableBindings(dto.Channel2Item);
    }

    private static void AssertChannelDrawableBindings(FourierSideChannelSpecularBlockerDTOItem channel)
    {
        channel.Document.ImageModel.Should().HaveCount(2);
        channel.PMTDocument.ImageModel.Should().HaveCount(2);
        channel.PMTDocument.ROIModel.Should().BeEmpty();
        channel.Document.ROIModel.Should().HaveCount(channel.Rods.Length * 2);
        channel.Document.ROIModel.Take(channel.Rods.Length).Should().Equal(channel.Rods.Select(t => t.BitmapImageROIDrawable));
        var step0Fourier = channel.Document.ImageModel.First();
        var step1Fourier = channel.Document.ImageModel.ElementAt(1);
        foreach (var (rod, step1ROI) in channel.Rods.Zip(channel.Document.ROIModel.Skip(channel.Rods.Length)))
        {
            rod.BitmapImageROIDrawable.BitmapImageDrawable.Should().BeSameAs(step0Fourier);
            rod.BitmapImageROIDrawable.BitmapImageDrawable.BitmapImage.Should().BeNull();
            rod.BitmapImageROIDrawable.Text.Should().Be($"{rod.Index + 1}");
            rod.BitmapImageROIDrawable.ResizeJoystickStateEnum.Should().Be(BitmapImageROIResizeJoystickStateEnum.XCenterYMin);
            step1ROI.BitmapImageDrawable.Should().BeSameAs(step1Fourier);
        }
    }

    private static FourierSideChannelSpecularBlockerDTO Deserialize(string json, JsonSerializerSettings settings)
    {
        var payload = JObject.Parse(json);
        payload.Remove(nameof(FourierSideChannelSpecularBlockerDTO.ProductivityInformation));
        return Guard.IsNotNullAndReturn(JsonConvert.DeserializeObject<FourierSideChannelSpecularBlockerDTO>(payload.ToString(), settings));
    }

    private static string RandomPath(Random random, string name) => $@"C:\Rnd\{random.Next(1000, 9999)}\{name}_{random.Next(0, 99)}.jpg";

    private static Rect RandomRect(Random random) => random.Next(6) switch
    {
        0 => Rect.Empty,
        1 => new Rect(NextCoordinate(random), NextCoordinate(random), 0d, 0d),
        _ => new Rect(NextCoordinate(random), NextCoordinate(random), Math.Abs(NextCoordinate(random)), Math.Abs(NextCoordinate(random)))
    };

    private static double NextCoordinate(Random random) => Math.Round((random.NextDouble() - 0.5d) * 4000d, 3);

    private static bool NextBool(Random random) => random.Next(2) == 1;

    private static long NextInt64(Random random) => ((long)random.Next() << 31) ^ random.Next();

    private static bool IsNumber(JValue value) => value.Type is JTokenType.Integer or JTokenType.Float;
}
