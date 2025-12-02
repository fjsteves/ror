using System.Text.Json;
using System.Text.Json.Serialization;
using RealmOfReality.Shared.Core;

namespace RealmOfReality.Shared.Serialization;

/// <summary>
/// JSON serialization configuration for game data
/// </summary>
public static class JsonConfig
{
    private static JsonSerializerOptions? _defaultOptions;
    private static JsonSerializerOptions? _prettyOptions;
    
    /// <summary>
    /// Default options for compact JSON (network/storage)
    /// </summary>
    public static JsonSerializerOptions Default => _defaultOptions ??= CreateOptions(false);
    
    /// <summary>
    /// Options for human-readable JSON (config files)
    /// </summary>
    public static JsonSerializerOptions Pretty => _prettyOptions ??= CreateOptions(true);
    
    private static JsonSerializerOptions CreateOptions(bool pretty)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = pretty,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new TilePositionConverter(),
                new WorldPositionConverter(),
                new EntityIdConverter(),
                new AccountIdConverter(),
                new CharacterIdConverter(),
                new ColorConverter(),
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
        return options;
    }
    
    public static string Serialize<T>(T value, bool pretty = false) =>
        JsonSerializer.Serialize(value, pretty ? Pretty : Default);
    
    public static T? Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Default);
    
    public static async Task<T?> DeserializeFileAsync<T>(string path)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, Default);
    }
    
    public static async Task SerializeFileAsync<T>(string path, T value, bool pretty = true)
    {
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, value, pretty ? Pretty : Default);
    }
}

// Custom JSON converters for game types

public class TilePositionConverter : JsonConverter<TilePosition>
{
    public override TilePosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            int x = 0, y = 0;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var prop = reader.GetString();
                    reader.Read();
                    if (prop == "x") x = reader.GetInt32();
                    else if (prop == "y") y = reader.GetInt32();
                }
            }
            return new TilePosition(x, y);
        }
        
        // Also support array format: [x, y]
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            reader.Read();
            var x = reader.GetInt32();
            reader.Read();
            var y = reader.GetInt32();
            reader.Read(); // End array
            return new TilePosition(x, y);
        }
        
        throw new JsonException("Invalid TilePosition format");
    }
    
    public override void Write(Utf8JsonWriter writer, TilePosition value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteEndObject();
    }
}

public class WorldPositionConverter : JsonConverter<WorldPosition>
{
    public override WorldPosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            float x = 0, y = 0, z = 0;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var prop = reader.GetString();
                    reader.Read();
                    if (prop == "x") x = reader.GetSingle();
                    else if (prop == "y") y = reader.GetSingle();
                    else if (prop == "z") z = reader.GetSingle();
                }
            }
            return new WorldPosition(x, y, z);
        }
        
        throw new JsonException("Invalid WorldPosition format");
    }
    
    public override void Write(Utf8JsonWriter writer, WorldPosition value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteNumber("z", value.Z);
        writer.WriteEndObject();
    }
}

public class EntityIdConverter : JsonConverter<EntityId>
{
    public override EntityId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString()!;
            if (str.StartsWith("E:"))
                return new EntityId(Convert.ToUInt64(str[2..], 16));
            return new EntityId(ulong.Parse(str));
        }
        return new EntityId(reader.GetUInt64());
    }
    
    public override void Write(Utf8JsonWriter writer, EntityId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class AccountIdConverter : JsonConverter<AccountId>
{
    public override AccountId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString()!;
            if (str.StartsWith("A:"))
                return new AccountId(uint.Parse(str[2..]));
            return new AccountId(uint.Parse(str));
        }
        return new AccountId(reader.GetUInt32());
    }
    
    public override void Write(Utf8JsonWriter writer, AccountId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class CharacterIdConverter : JsonConverter<CharacterId>
{
    public override CharacterId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString()!;
            if (str.StartsWith("C:"))
                return new CharacterId(uint.Parse(str[2..]));
            return new CharacterId(uint.Parse(str));
        }
        return new CharacterId(reader.GetUInt32());
    }
    
    public override void Write(Utf8JsonWriter writer, CharacterId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class ColorConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString()!;
            // Support hex format: "#RRGGBB" or "#AARRGGBB"
            if (str.StartsWith("#"))
            {
                str = str[1..];
                if (str.Length == 6)
                    return new Color(
                        Convert.ToByte(str[..2], 16),
                        Convert.ToByte(str[2..4], 16),
                        Convert.ToByte(str[4..6], 16));
                if (str.Length == 8)
                    return new Color(
                        Convert.ToByte(str[2..4], 16),
                        Convert.ToByte(str[4..6], 16),
                        Convert.ToByte(str[6..8], 16),
                        Convert.ToByte(str[..2], 16));
            }
        }
        
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            byte r = 0, g = 0, b = 0, a = 255;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var prop = reader.GetString();
                    reader.Read();
                    if (prop == "r") r = reader.GetByte();
                    else if (prop == "g") g = reader.GetByte();
                    else if (prop == "b") b = reader.GetByte();
                    else if (prop == "a") a = reader.GetByte();
                }
            }
            return new Color(r, g, b, a);
        }
        
        throw new JsonException("Invalid Color format");
    }
    
    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        if (value.A == 255)
            writer.WriteStringValue($"#{value.R:X2}{value.G:X2}{value.B:X2}");
        else
            writer.WriteStringValue($"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}");
    }
}
