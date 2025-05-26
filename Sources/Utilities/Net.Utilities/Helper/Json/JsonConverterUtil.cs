using Net.Utilities.Helper.Struct;
using Newtonsoft.Json;

namespace Net.Utilities.Helper.Json;

public static class JsonConverterUtil
{
    /// <summary>
    /// Json数据返回到前端js的时候，把数值很大的long类型转成字符串
    /// </summary>
    public class LongJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            return long.TryParse(reader.Value?.ToString(), out var result) ? result : 0;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteValue(value.ToString());
        }
    }

    /// <summary>
    /// DateTime类型序列化的时候，转成指定的格式
    /// </summary>
    public class DateTimeJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            return DateTimeHelper.String2DateTime(reader.Value?.ToString()) ?? DateTime.MinValue;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is not DateTime dateTime)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteValue(DateTimeHelper.DateTime2String(dateTime));
        }
    }
}