using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Core.Utilities;

public sealed class PrivateSetterContractResolver : DefaultContractResolver
{
    public static readonly JsonSerializerSettings PrivateSetterAndReplaceSettings = new()
    {
        ContractResolver = new PrivateSetterContractResolver(),
        ObjectCreationHandling = ObjectCreationHandling.Replace
    };

    public static readonly JsonSerializer PrivateSetterAndReplaceJsonSerializer = new()
    {
        ContractResolver = new PrivateSetterContractResolver(),
        ObjectCreationHandling = ObjectCreationHandling.Replace
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