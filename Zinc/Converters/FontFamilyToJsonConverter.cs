using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zinc.Converters;

public class FontFamilyToJsonConverter : JsonConverter<FontFamily>
{

    public override FontFamily Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new FontFamily(reader.GetString() ?? "Segoe UI");

    public override void Write(Utf8JsonWriter writer, FontFamily value, JsonSerializerOptions options)
    {
        // 优先写入 Key（如果有），以保留完整 avares URI（包含文件名），否则退回到 Name
        var key = value.Key?.ToString();
        if (!string.IsNullOrEmpty(key))
        {
            writer.WriteStringValue(key.Replace("compositefont:", ""));
        }
        else
        {
            writer.WriteStringValue(value.Name);
        }
    }
}