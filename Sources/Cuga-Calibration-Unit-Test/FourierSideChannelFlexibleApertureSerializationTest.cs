using AwesomeAssertions;
using AwesomeAssertions.Execution;
using CommunityToolkit.Diagnostics;
using Core.Models.Models.Fourier.SideChannelFlexibleAperture;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Reflection;

namespace CugaCalibrationUnitTest;

public sealed class FourierSideChannelFlexibleApertureSerializationTest
{
    private static readonly string[] NonPersistentNames = ["Document", "BitmapImageROIDrawable", "document", "bitmapImageROIDrawable"];
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

    private static JObject LoadProvidedJson() => JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "FourierSideChannelFlexibleAperture", "Json_1.json")));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProvidedJson_ShouldMatchEveryField_AndSerializeBackIdentically(bool useCacheSettings)
    {
        var settings = useCacheSettings ? PrivateSetterContractResolver.Settings : new JsonSerializerSettings();
        var provided = LoadProvidedJson();

        using var restored = Guard.IsNotNullAndReturn(JsonConvert.DeserializeObject<FourierSideChannelFlexibleApertureDTO>(provided.ToString(), settings));
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

        using var restored = Guard.IsNotNullAndReturn(JsonConvert.DeserializeObject<FourierSideChannelFlexibleApertureDTO>(json, settings));
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

    private static FourierSideChannelFlexibleApertureDTO CreateRandomDto(Random random, int rodTotalCount)
    {
        var dto = new FourierSideChannelFlexibleApertureDTO(rodTotalCount, RandomPath(random), RandomPath(random))
        {
            IsCalibrated = NextBool(random),
            IsVerified = NextBool(random),
            IsRequiredSelfCheck = NextBool(random),
            Id = NextInt64(random),
            Expiration = NextInt64(random)
        };

        foreach (var channel in new[] { dto.Channel1Item, dto.Channel2Item })
        {
            channel.MinMotorAbsoluteValue = NextCoordinate(random);
            channel.MaxMotorAbsoluteValue = NextCoordinate(random);
            foreach (var item in new[] { channel.EvenItem, channel.OddItem })
            {
                item.Step0AndStep1MotorAbsoluteValue = NextCoordinate(random);
                item.Step2MotorAbsoluteValue = NextCoordinate(random);
                item.Step0ChannelImageFilePath = RandomPath(random);
                item.Step1ChannelImageFilePath = RandomPath(random);
                item.Step2ChannelImageFilePath = RandomPath(random);
                foreach (var rod in RodsOf(item))
                {
                    rod.IsDeleted = NextBool(random);
                    rod.ImageROI = RandomRect(random);
                }
            }

            foreach (var rod in channel.RodResults)
            {
                rod.IsDeleted = NextBool(random);
                rod.PixelSize = NextCoordinate(random);
                rod.MinImageROI = RandomRect(random);
                rod.MaxImageROI = RandomRect(random);
            }
        }

        return dto;
    }

    private static void AssertDtoMatchesJson(JObject expected, FourierSideChannelFlexibleApertureDTO actual)
    {
        using var scope = new AssertionScope("DTO");
        AssertPrivateFieldEqualsJson(expected, actual, "_rodTotalCount");
        actual.IsCalibrated.Should().Be(expected.Value<bool>(nameof(actual.IsCalibrated)));
        actual.IsVerified.Should().Be(expected.Value<bool>(nameof(actual.IsVerified)));
        actual.IsRequiredSelfCheck.Should().Be(expected.Value<bool>(nameof(actual.IsRequiredSelfCheck)));
        actual.IsOk.Should().Be(expected.Value<bool>(nameof(actual.IsOk)));
        if (expected[nameof(actual.Id)] is not null) actual.Id.Should().Be(expected.Value<long>(nameof(actual.Id)));
        if (expected[nameof(actual.Expiration)] is not null) actual.Expiration.Should().Be(expected.Value<long>(nameof(actual.Expiration)));
        AssertChannelMatchesJson(RequiredJson<JObject>(expected[nameof(actual.Channel1Item)]), actual.Channel1Item);
        AssertChannelMatchesJson(RequiredJson<JObject>(expected[nameof(actual.Channel2Item)]), actual.Channel2Item);
    }

    private static void AssertChannelMatchesJson(JObject expected, FourierSideChannelFlexibleApertureDTOItem actual)
    {
        using var scope = new AssertionScope(expected.Path);
        AssertPrivateFieldEqualsJson(expected, actual, "_rodTotalCount");
        actual.ChannelId.Should().Be(expected.Value<int>(nameof(actual.ChannelId)));
        actual.ChannelImageFilePath.Should().Be(expected.Value<string>(nameof(actual.ChannelImageFilePath)));
        actual.MinMotorAbsoluteValue.Should().Be(expected.Value<double>(nameof(actual.MinMotorAbsoluteValue)));
        actual.MaxMotorAbsoluteValue.Should().Be(expected.Value<double>(nameof(actual.MaxMotorAbsoluteValue)));
        AssertItemMatchesJson(RequiredJson<JObject>(expected[nameof(actual.EvenItem)]), actual.EvenItem);
        AssertItemMatchesJson(RequiredJson<JObject>(expected[nameof(actual.OddItem)]), actual.OddItem);
        AssertRodResultsMatchJson(RequiredJson<JArray>(expected[nameof(actual.RodResults)]), actual.RodResults);
    }

    private static void AssertItemMatchesJson(JObject expected, FourierSideChannelFlexibleApertureDTOItem.Item actual)
    {
        using var scope = new AssertionScope(expected.Path);
        AssertPrivateFieldEqualsJson(expected, actual, "_rodTotalCount");
        AssertPrivateFieldEqualsJson(expected, actual, "_isEven");
        actual.Step0AndStep1MotorAbsoluteValue.Should().Be(expected.Value<double>(nameof(actual.Step0AndStep1MotorAbsoluteValue)));
        actual.Step2MotorAbsoluteValue.Should().Be(expected.Value<double>(nameof(actual.Step2MotorAbsoluteValue)));
        actual.Step0ChannelImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step0ChannelImageFilePath)));
        actual.Step1ChannelImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step1ChannelImageFilePath)));
        actual.Step2ChannelImageFilePath.Should().Be(expected.Value<string>(nameof(actual.Step2ChannelImageFilePath)));
        AssertRodMatchesJson(RequiredJson<JToken>(expected[nameof(actual.Step0LeftRod)]), actual.Step0LeftRod);
        AssertRodMatchesJson(RequiredJson<JToken>(expected[nameof(actual.Step0RightRod)]), actual.Step0RightRod);
        AssertRodsMatchJson(RequiredJson<JArray>(expected[nameof(actual.Step1Rods)]), actual.Step1Rods);
        AssertRodsMatchJson(RequiredJson<JArray>(expected[nameof(actual.Step2Rods)]), actual.Step2Rods);
    }

    private static void AssertRodsMatchJson(JArray expected, FourierSideChannelFlexibleApertureDTOItem.Rod[] actual)
    {
        actual.Select(t => t.Index).Should().BeEquivalentTo(expected.Select(t => t.Value<int>("Index")), "{0} rod indexes", expected.Path);
        foreach (var token in expected)
            AssertRodMatchesJson(token, actual.Single(t => t.Index == token.Value<int>("Index")));
    }

    private static void AssertRodResultsMatchJson(JArray expected, FourierSideChannelFlexibleApertureDTOItem.RodResult[] actual)
    {
        actual.Select(t => t.Index).Should().BeEquivalentTo(expected.Select(t => t.Value<int>("Index")), "{0} rod result indexes", expected.Path);
        foreach (var token in expected)
        {
            var rod = actual.Single(t => t.Index == token.Value<int>("Index"));
            using var rodScope = new AssertionScope(token.Path);
            rod.Index.Should().Be(token.Value<int>(nameof(rod.Index)));
            rod.IsDeleted.Should().Be(token.Value<bool>(nameof(rod.IsDeleted)));
            rod.PixelSize.Should().Be(token.Value<double>(nameof(rod.PixelSize)));
            AssertRectMatchesJson(RequiredJson<JToken>(token[nameof(rod.MinImageROI)]), rod.MinImageROI);
            AssertRectMatchesJson(RequiredJson<JToken>(token[nameof(rod.MaxImageROI)]), rod.MaxImageROI);
        }
    }

    private static void AssertRodMatchesJson(JToken expected, FourierSideChannelFlexibleApertureDTOItem.Rod actual)
    {
        using var scope = new AssertionScope(expected.Path);
        actual.Index.Should().Be(expected.Value<int>(nameof(actual.Index)));
        actual.IsDeleted.Should().Be(expected.Value<bool>(nameof(actual.IsDeleted)));
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

    private static void AssertDtoEquals(FourierSideChannelFlexibleApertureDTO expected, FourierSideChannelFlexibleApertureDTO actual)
    {
        using var scope = new AssertionScope("DTO");
        GetPrivateField(actual, "_rodTotalCount").Should().Be(GetPrivateField(expected, "_rodTotalCount"));
        actual.IsCalibrated.Should().Be(expected.IsCalibrated);
        actual.IsVerified.Should().Be(expected.IsVerified);
        actual.IsRequiredSelfCheck.Should().Be(expected.IsRequiredSelfCheck);
        actual.IsOk.Should().Be(expected.IsOk);
        actual.Id.Should().Be(expected.Id);
        actual.Expiration.Should().Be(expected.Expiration);
        AssertChannelEquals(expected.Channel1Item, actual.Channel1Item);
        AssertChannelEquals(expected.Channel2Item, actual.Channel2Item);
    }

    private static void AssertChannelEquals(FourierSideChannelFlexibleApertureDTOItem expected, FourierSideChannelFlexibleApertureDTOItem actual)
    {
        GetPrivateField(actual, "_rodTotalCount").Should().Be(GetPrivateField(expected, "_rodTotalCount"));
        actual.ChannelId.Should().Be(expected.ChannelId);
        actual.ChannelImageFilePath.Should().Be(expected.ChannelImageFilePath);
        actual.MinMotorAbsoluteValue.Should().Be(expected.MinMotorAbsoluteValue);
        actual.MaxMotorAbsoluteValue.Should().Be(expected.MaxMotorAbsoluteValue);
        AssertItemEquals(expected.EvenItem, actual.EvenItem);
        AssertItemEquals(expected.OddItem, actual.OddItem);
        actual.RodResults.Select(t => t.Index).Should().Equal(expected.RodResults.Select(t => t.Index));
        foreach (var source in expected.RodResults)
        {
            var rod = actual.RodResults.Single(t => t.Index == source.Index);
            rod.IsDeleted.Should().Be(source.IsDeleted);
            rod.PixelSize.Should().Be(source.PixelSize);
            rod.MinImageROI.Should().Be(source.MinImageROI);
            rod.MaxImageROI.Should().Be(source.MaxImageROI);
        }
    }

    private static void AssertItemEquals(FourierSideChannelFlexibleApertureDTOItem.Item expected, FourierSideChannelFlexibleApertureDTOItem.Item actual)
    {
        GetPrivateField(actual, "_rodTotalCount").Should().Be(GetPrivateField(expected, "_rodTotalCount"));
        GetPrivateField(actual, "_isEven").Should().Be(GetPrivateField(expected, "_isEven"));
        actual.Step0AndStep1MotorAbsoluteValue.Should().Be(expected.Step0AndStep1MotorAbsoluteValue);
        actual.Step2MotorAbsoluteValue.Should().Be(expected.Step2MotorAbsoluteValue);
        actual.Step0ChannelImageFilePath.Should().Be(expected.Step0ChannelImageFilePath);
        actual.Step1ChannelImageFilePath.Should().Be(expected.Step1ChannelImageFilePath);
        actual.Step2ChannelImageFilePath.Should().Be(expected.Step2ChannelImageFilePath);
        AssertRodEquals(expected.Step0LeftRod, actual.Step0LeftRod);
        AssertRodEquals(expected.Step0RightRod, actual.Step0RightRod);
        actual.Step1Rods.Select(t => t.Index).Should().Equal(expected.Step1Rods.Select(t => t.Index));
        actual.Step2Rods.Select(t => t.Index).Should().Equal(expected.Step2Rods.Select(t => t.Index));
        foreach (var source in expected.Step1Rods)
            AssertRodEquals(source, actual.Step1Rods.Single(t => t.Index == source.Index));
        foreach (var source in expected.Step2Rods)
            AssertRodEquals(source, actual.Step2Rods.Single(t => t.Index == source.Index));
    }

    private static void AssertRodEquals(FourierSideChannelFlexibleApertureDTOItem.Rod expected, FourierSideChannelFlexibleApertureDTOItem.Rod actual)
    {
        actual.Index.Should().Be(expected.Index);
        actual.IsDeleted.Should().Be(expected.IsDeleted);
        actual.ImageROI.Should().Be(expected.ImageROI);
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

    private static void AssertDrawableBindings(FourierSideChannelFlexibleApertureDTO dto)
    {
        AssertChannelDrawableBindings(dto.Channel1Item);
        AssertChannelDrawableBindings(dto.Channel2Item);
    }

    private static void AssertChannelDrawableBindings(FourierSideChannelFlexibleApertureDTOItem channel)
    {
        channel.Document.ImageModel.Should().HaveCount(1);
        channel.Document.ROIModel.Should().Equal(channel.RodResults.Select(t => t.BitmapImageROIDrawable));
        foreach (var rod in channel.RodResults)
        {
            rod.BitmapImageROIDrawable.BitmapImageDrawable.Should().BeSameAs(channel.Document.ImageModel.Single());
            rod.BitmapImageROIDrawable.BitmapImageDrawable.BitmapImage.Should().BeNull();
            rod.BitmapImageROIDrawable.Text.Should().Be($"{rod.Index + 1}");
        }

        foreach (var item in new[] { channel.EvenItem, channel.OddItem }) AssertItemDrawableBindings(item);
    }

    private static void AssertItemDrawableBindings(FourierSideChannelFlexibleApertureDTOItem.Item item)
    {
        var rods = RodsOf(item).ToArray();
        item.Document.ROIModel.Should().Equal(rods.Select(t => t.BitmapImageROIDrawable));
        item.Document.ImageModel.Should().HaveCount(3);
        var images = item.Document.ImageModel.ToArray();
        item.Step0LeftRod.BitmapImageROIDrawable.BitmapImageDrawable.Should().BeSameAs(images[0]);
        item.Step0RightRod.BitmapImageROIDrawable.BitmapImageDrawable.Should().BeSameAs(images[0]);
        foreach (var rod in item.Step1Rods) rod.BitmapImageROIDrawable.BitmapImageDrawable.Should().BeSameAs(images[1]);
        foreach (var rod in item.Step2Rods) rod.BitmapImageROIDrawable.BitmapImageDrawable.Should().BeSameAs(images[2]);

        var step1Handles = BitmapImageROIResizeJoystickStateEnum.XMinYMin
                           | BitmapImageROIResizeJoystickStateEnum.XCenterYMin
                           | BitmapImageROIResizeJoystickStateEnum.XMaxYMin;
        foreach (var rod in new[] { item.Step0LeftRod, item.Step0RightRod }.Concat(item.Step1Rods))
            rod.BitmapImageROIDrawable.ResizeJoystickStateEnum.Should().Be(step1Handles);
        foreach (var rod in item.Step2Rods)
            rod.BitmapImageROIDrawable.ResizeJoystickStateEnum.Should().Be(BitmapImageROIResizeJoystickStateEnum.XCenterYMin);
        foreach (var rod in rods)
        {
            rod.BitmapImageROIDrawable.Text.Should().Be($"{rod.Index + 1}");
            rod.BitmapImageROIDrawable.BitmapImageDrawable.BitmapImage.Should().BeNull();
        }
    }

    private static IEnumerable<FourierSideChannelFlexibleApertureDTOItem.Rod> RodsOf(FourierSideChannelFlexibleApertureDTOItem.Item item) =>
        new[] { item.Step0LeftRod, item.Step0RightRod }.Concat(item.Step1Rods).Concat(item.Step2Rods);

    private static string RandomPath(Random random) => $@"C:\Rnd\{random.Next(1000, 9999)}\step_{random.Next(0, 99)}_{random.Next(0, 99)}.jpg";

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
