using Avalonia.Media;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zinc.Converters;

public sealed class FontFamilyToJsonConverter : JsonConverter<FontFamily>
{
    /// <summary>复合字体 URI 的前缀，写入设置时需要去掉。</summary>
    private const string CompositeFontPrefix = "compositefont:";

    public override FontFamily Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();

        return string.IsNullOrWhiteSpace(text)
            ? FontFamily.Default
            : new FontFamily(text);
    }

    public override void Write(Utf8JsonWriter writer, FontFamily value, JsonSerializerOptions options)
    {
        // 优先写入 Key（如果有），以保留完整 avares URI（包含文件名），否则退回到 Name
        var key = value.Key?.ToString();

        if (!string.IsNullOrEmpty(key))
        {
            var text = key.StartsWith(CompositeFontPrefix, StringComparison.Ordinal)
                ? key[CompositeFontPrefix.Length..]
                : key;

            // FontFamilyKey 只保留到文件为止，会丢掉 “#字体族” 片段；
            // 缺失时用 Name 补回，否则读回的 FontFamily 与原值不相等。
            if (!text.Contains('#') && !string.IsNullOrWhiteSpace(value.Name))
            {
                text = $"{text}#{value.Name}";
            }

            writer.WriteStringValue(text);
            return;
        }

        writer.WriteStringValue(value.Name);
    }
}
