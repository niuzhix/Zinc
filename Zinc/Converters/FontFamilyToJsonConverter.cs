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
        => writer.WriteStringValue(value.Name);
}