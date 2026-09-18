using AwesomeAssertions;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Models.Serializations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CugaCalibrationUnitTest.Serializations;

public sealed class PrivateSetterContractResolverSerializationTest
{
    [Fact]
    public void FooSerialization_ShouldOnlySerializePublicProperties_AndSupportPrivateSetters()
    {
        var foo = new Foo(
            22,
            33,
            44,
            55)
        {
            PublicPropertyPublicSetInt = 11
        };

        var json = JsonConvert.SerializeObject(foo, PrivateSetterContractResolver.Settings);
        var deserialized = JsonConvert.DeserializeObject<Foo>(json, PrivateSetterContractResolver.Settings);

        var saved = JObject.Parse(json);
        saved.Descendants().OfType<JProperty>().Select(t => t.Name).Should().BeEquivalentTo(
                nameof(Foo.PublicPropertyPublicSetInt),
                nameof(Foo.PublicPropertyPrivateSetInt),
                nameof(Foo.PublicPropertyInternalSetInt),
                nameof(Foo.PublicPropertyReadOnlyInt))
            .And.NotContain("PrivatePropertyPrivateSetInt");

        deserialized.Should().NotBeNull();
        deserialized.PublicPropertyPublicSetInt.Should().Be(foo.PublicPropertyPublicSetInt);
        deserialized.PublicPropertyPrivateSetInt.Should().Be(foo.PublicPropertyPrivateSetInt);
        deserialized.PublicPropertyInternalSetInt.Should().Be(foo.PublicPropertyInternalSetInt);
        deserialized.PublicPropertyReadOnlyInt.Should().Be(4)
            .And.NotBe(foo.PublicPropertyReadOnlyInt);
        ObjectHelper.GetPropertyValue(deserialized, "PrivatePropertyPrivateSetInt").Should().Be(5)
            .And.NotBe(ObjectHelper.GetPropertyValue(foo, "PrivatePropertyPrivateSetInt"));
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

    [Fact]
    public void Foo1Serialization_ShouldRestoreReadOnlyProperty_ViaJsonConstructor_AndKeepPrivatePropertyDefault()
    {
        var foo1 = new Foo1(
            22,
            33,
            44,
            55
        )
        {
            PublicPropertyPublicSetInt = 11
        };

        var json = JsonConvert.SerializeObject(foo1, PrivateSetterContractResolver.Settings);
        var deserialized = JsonConvert.DeserializeObject<Foo1>(json, PrivateSetterContractResolver.Settings);

        var saved = JObject.Parse(json);
        saved.Descendants().OfType<JProperty>().Select(t => t.Name).Should().BeEquivalentTo(
            nameof(Foo1.PublicPropertyPublicSetInt),
            nameof(Foo1.PublicPropertyPrivateSetInt),
            nameof(Foo1.PublicPropertyInternalSetInt),
            nameof(Foo1.PublicPropertyReadOnlyInt),
            "PrivatePropertyPrivateSetInt");

        deserialized.Should().NotBeNull();
        deserialized.PublicPropertyPublicSetInt.Should().Be(foo1.PublicPropertyPublicSetInt);
        deserialized.PublicPropertyPrivateSetInt.Should().Be(foo1.PublicPropertyPrivateSetInt);
        deserialized.PublicPropertyInternalSetInt.Should().Be(foo1.PublicPropertyInternalSetInt);
        deserialized.PublicPropertyReadOnlyInt.Should().Be(foo1.PublicPropertyReadOnlyInt)
            .And.NotBe(4);
        ObjectHelper.GetPropertyValue(deserialized, "PrivatePropertyPrivateSetInt").Should()
            .Be(ObjectHelper.GetPropertyValue(foo1, "PrivatePropertyPrivateSetInt"))
            .And.NotBe(5);
    }

    public sealed class Foo1
    {
        public int PublicPropertyPublicSetInt { get; set; } = 1;

        public int PublicPropertyPrivateSetInt { get; private set; } = 2;

        public int PublicPropertyInternalSetInt { get; internal set; } = 3;

        public int PublicPropertyReadOnlyInt { get; } = 4;

        [JsonProperty]
        private int PrivatePropertyPrivateSetInt { get; set; } = 5;

        public Foo1()
        {
        }

        [JsonConstructor]
        public Foo1(
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