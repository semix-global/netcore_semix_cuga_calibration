using Net.Utilities.Helper.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GRPC.CodeFirst.Shared;

[ProtoContract]
public sealed class Person
{
    [ProtoMember(1)]
    public int Id { get; init; }

    [ProtoMember(2)]
    public Guid Guid { get; set; }

    [ProtoMember(3)]
    public required string Name { get; set; }

    [ProtoMember(4)]
    [JsonConverter(typeof(StringEnumConverter))]
    public TagEnum TagEnum { get; set; }

    [ProtoMember(5)]
    [JsonConverter(typeof(JsonConverterUtil.DateTimeJsonConverter))]
    public DateTime DateTime { get; set; }

    [ProtoMember(6)]
    public required Address Address { get; init; }

    /*[ProtoMember(7)]
    public Ulid Ulid { get; set; }*/
}

[ProtoContract]
public sealed class Address
{
    [ProtoMember(1)]
    public required string Line1 { get; set; }

    [ProtoMember(2)]
    public required string Line2 { get; set; }

    [ProtoMember(3)]
    public required byte[] Image { get; init; }
}

[Flags]
public enum TagEnum
{
    Tag1 = 0x0000_0001,
    Tag2 = 0x0000_0010,
    Tag3 = 0x0000_0100
}

[ProtoContract]
public sealed class IntWrapper
{
    [ProtoMember(1)]
    public int Value { get; init; }
}

[ProtoContract]
public sealed class ParamWrapper
{
    [ProtoMember(1)]
    public int? Id { get; init; }

    [ProtoMember(2)]
    public DateTime? DateTime { get; init; }

    [ProtoMember(3)]
    public Guid? Guid { get; init; }

    [ProtoMember(4)]
    public string? Name { get; init; }

    [ProtoMember(5)]
    [JsonConverter(typeof(StringEnumConverter))]
    public TagEnum? TagEnum { get; init; }

    [ProtoMember(6)]
    public byte[]? Image { get; init; }

    [ProtoMember(7)]
    public bool IsLarge { get; init; }

    [ProtoMember(8)]
    public bool IsRandom { get; init; }
}

[ProtoContract]
public class SxExecuteRet<T>
{
    [ProtoMember(1)]
    public bool Success { get; set; } = true;

    [ProtoMember(2)]
    public string Msg { get; set; } = string.Empty;

    [ProtoIgnore]
    public Exception? Exception { get; set; }

    [ProtoMember(4)]
    public required T Anything { get; set; }

    [ProtoIgnore]
    public bool IsSuccess => Success && Exception == null;

    [ProtoIgnore]
    public string ErrorMsg => Exception == null ? Msg : Exception.Message;

    public void ThrowIfException()
    {
        if (Success)
            return;

        if (Exception == null)
            throw new Exception(Msg);
        throw Exception;
    }
}

[ProtoContract]
public sealed class TestObj
{
    [ProtoMember(1)]
    public bool Success { get; set; } = true;

    [ProtoMember(2)]
    public string Msg { get; set; } = string.Empty;

    [ProtoMember(3)]
    public List<string> Tes { get; set; } = [];
}

[ProtoContract]
public sealed class TestObj1
{
    [ProtoMember(1)]
    public int Id { get; set; }

    [ProtoMember(2)]
    public string? Name { get; set; }
}

[Service]
public interface IPersonService
{
    Person Get(ParamWrapper paramWrapper);

    Task<Person> GetCallContextAsync(IntWrapper intWrapper, CallContext context = default);

    Task<Person> GetCancellationTokenAsync(IntWrapper intWrapper, CancellationToken context = default);

    SxExecuteRet<List<int>> GetList();

    SxExecuteRet<List<Person>> GetListByParam(TestObj testObj);

    Task<SxExecuteRet<TestObj>> GetListByParam1Async(CancellationToken context = default);

    Task<SxExecuteRet<SxExecuteRet<TestObj>>> GetListByParam2Async(CancellationToken context = default);

    SxExecuteRet<List<TestObj1>> GetList1();
}