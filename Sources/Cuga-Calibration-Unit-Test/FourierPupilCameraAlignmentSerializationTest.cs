using AwesomeAssertions;
using AwesomeAssertions.Execution;
using CommunityToolkit.Diagnostics;
using Core.Models.Models.Fourier.PupilCameraAlignment;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace CugaCalibrationUnitTest;

public sealed class FourierPupilCameraAlignmentSerializationTest
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

    private static JObject LoadProvidedJson() => JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "FourierPupilCameraAlignment", "Json_1.json")));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProvidedJson_ShouldMatchEveryField_AndSerializeBackIdentically(bool useCacheSettings)
    {
        var settings = useCacheSettings ? PrivateSetterContractResolver.Settings : new JsonSerializerSettings();
        var provided = LoadProvidedJson();

        using var restored = Guard.IsNotNullAndReturn(JsonConvert.DeserializeObject<FourierPupilCameraAlignmentDTO>(provided.ToString(), settings));
        AssertDtoMatchesJson(provided, restored);
        AssertDrawableBindings(restored);

        var saved = JObject.Parse(JsonConvert.SerializeObject(restored, settings));
        saved.Descendants().OfType<JProperty>().Select(t => t.Name).Should().NotContain(NonPersistentNames);
        AssertJsonIdentical(provided, saved);
    }

    [Theory]
    [InlineData(11, false)]
    [InlineData(22, true)]
    [InlineData(33, false)]
    [InlineData(44, true)]
    [InlineData(55, false)]
    public void RandomDto_ShouldMatchEveryField_AfterJsonRoundTrip(int seed, bool useCacheSettings)
    {
        var settings = useCacheSettings ? PrivateSetterContractResolver.Settings : new JsonSerializerSettings();
        using var source = CreateRandomDto(new Random(seed));
        var json = JsonConvert.SerializeObject(source, settings);

        using var restored = Guard.IsNotNullAndReturn(JsonConvert.DeserializeObject<FourierPupilCameraAlignmentDTO>(json, settings));
        AssertDtoEquals(source, restored);
        AssertDrawableBindings(restored);
    }

    [Fact]
    public void RandomDto_ShouldMatchEveryField_AfterJsonRoundTrip_WithNewSeed()
    {
        var seed = new Random().Next();
        using var scope = new AssertionScope($"seed={seed}");
        RandomDto_ShouldMatchEveryField_AfterJsonRoundTrip(seed, false);
    }

    private static FourierPupilCameraAlignmentDTO CreateRandomDto(Random random)
    {
        var dto = new FourierPupilCameraAlignmentDTO
        {
            IsCalibrated = NextBool(random),
            IsVerified = NextBool(random),
            IsRequiredSelfCheck = NextBool(random),
            Id = NextInt64(random),
            Expiration = NextInt64(random)
        };

        foreach (var channel in new[] { dto.Channel1Item, dto.Channel2Item, dto.Channel3Item })
        {
            channel.ChannelImageFilePath = RandomPath(random, "Channel");
            channel.ROIChannelImageFilePath = RandomPath(random, "ROI");
            channel.ImageROI = RandomRect(random);
        }

        return dto;
    }

    private static void AssertDtoMatchesJson(JObject expected, FourierPupilCameraAlignmentDTO actual)
    {
        using var scope = new AssertionScope("DTO");
        actual.IsCalibrated.Should().Be(expected.Value<bool>(nameof(actual.IsCalibrated)));
        actual.IsVerified.Should().Be(expected.Value<bool>(nameof(actual.IsVerified)));
        actual.IsRequiredSelfCheck.Should().Be(expected.Value<bool>(nameof(actual.IsRequiredSelfCheck)));
        actual.IsOk.Should().Be(expected.Value<bool>(nameof(actual.IsOk)));
        if (expected[nameof(actual.Id)] is not null) actual.Id.Should().Be(expected.Value<long>(nameof(actual.Id)));
        if (expected[nameof(actual.Expiration)] is not null) actual.Expiration.Should().Be(expected.Value<long>(nameof(actual.Expiration)));
        AssertChannelMatchesJson(RequiredJson<JObject>(expected[nameof(actual.Channel1Item)]), actual.Channel1Item);
        AssertChannelMatchesJson(RequiredJson<JObject>(expected[nameof(actual.Channel2Item)]), actual.Channel2Item);
        AssertChannelMatchesJson(RequiredJson<JObject>(expected[nameof(actual.Channel3Item)]), actual.Channel3Item);
    }

    private static void AssertChannelMatchesJson(JObject expected, FourierPupilCameraAlignmentDTOItem actual)
    {
        using var scope = new AssertionScope(expected.Path);
        actual.ChannelId.Should().Be(expected.Value<int>(nameof(actual.ChannelId)));
        actual.ChannelImageFilePath.Should().Be(expected.Value<string>(nameof(actual.ChannelImageFilePath)));
        actual.ROIChannelImageFilePath.Should().Be(expected.Value<string>(nameof(actual.ROIChannelImageFilePath)));
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

    private static void AssertDtoEquals(FourierPupilCameraAlignmentDTO expected, FourierPupilCameraAlignmentDTO actual)
    {
        using var scope = new AssertionScope("DTO");
        actual.IsCalibrated.Should().Be(expected.IsCalibrated);
        actual.IsVerified.Should().Be(expected.IsVerified);
        actual.IsRequiredSelfCheck.Should().Be(expected.IsRequiredSelfCheck);
        actual.IsOk.Should().Be(expected.IsOk);
        actual.Id.Should().Be(expected.Id);
        actual.Expiration.Should().Be(expected.Expiration);
        AssertChannelEquals(expected.Channel1Item, actual.Channel1Item);
        AssertChannelEquals(expected.Channel2Item, actual.Channel2Item);
        AssertChannelEquals(expected.Channel3Item, actual.Channel3Item);
    }

    private static void AssertChannelEquals(FourierPupilCameraAlignmentDTOItem expected, FourierPupilCameraAlignmentDTOItem actual)
    {
        actual.ChannelId.Should().Be(expected.ChannelId);
        actual.ChannelImageFilePath.Should().Be(expected.ChannelImageFilePath);
        actual.ROIChannelImageFilePath.Should().Be(expected.ROIChannelImageFilePath);
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

    private static T RequiredJson<T>(JToken? token) where T : JToken =>
        Guard.IsAssignableToTypeAndReturn<T>(Guard.IsNotNullAndReturn(token));

    private static void AssertDrawableBindings(FourierPupilCameraAlignmentDTO dto)
    {
        AssertChannelDrawableBindings(dto.Channel1Item);
        AssertChannelDrawableBindings(dto.Channel2Item);
        AssertChannelDrawableBindings(dto.Channel3Item);
    }

    private static void AssertChannelDrawableBindings(FourierPupilCameraAlignmentDTOItem channel)
    {
        channel.Document.ImageModel.Should().HaveCount(2);
        channel.Document.ROIModel.Should().HaveCount(1);
        var images = channel.Document.ImageModel.ToArray();
        var roi = channel.Document.ROIModel.Single();
        roi.BitmapImageDrawable.Should().BeSameAs(images[0]);
        roi.BitmapImageDrawable.BitmapImage.Should().BeNull();
        roi.ResizeJoystickStateEnum.Should().Be(BitmapImageROIResizeJoystickStateEnum.All);
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
