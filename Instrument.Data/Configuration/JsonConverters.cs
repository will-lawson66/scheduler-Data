using System.Text.Json;
using System.Text.Json.Serialization;
using Instrument.Data.Entities.Enums;

namespace Instrument.Data.Configuration;

public class TechnologyJsonConverter : JsonConverter<Technology?>
{
    public override Technology? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            
            if (Enum.TryParse<Technology>(value, ignoreCase: true, out var technology))
            {
                return technology;
            }
            
            throw new JsonException($"Unable to convert '{value}' to Technology enum");
        }
        
        throw new JsonException($"Unexpected token type {reader.TokenType} when parsing Technology");
    }

    public override void Write(Utf8JsonWriter writer, Technology? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString());
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (TimeSpan.TryParse(value, out var timeSpan))
            {
                return timeSpan;
            }
            throw new JsonException($"Unable to convert '{value}' to TimeSpan");
        }
        
        throw new JsonException($"Unexpected token type {reader.TokenType} when parsing TimeSpan");
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(@"hh\:mm\:ss"));
    }
}