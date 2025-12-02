using RealmOfReality.Shared.Serialization;

namespace RealmOfReality.Shared.Gumps;

/// <summary>
/// Serializable gump data that can be sent over network
/// </summary>
public class GumpData : ISerializable
{
    /// <summary>
    /// Unique type identifier for this gump type
    /// </summary>
    public uint GumpTypeId { get; set; }
    
    /// <summary>
    /// Instance serial for tracking responses
    /// </summary>
    public uint Serial { get; set; }
    
    /// <summary>
    /// Screen X position
    /// </summary>
    public int X { get; set; }
    
    /// <summary>
    /// Screen Y position
    /// </summary>
    public int Y { get; set; }
    
    /// <summary>
    /// Behavior flags
    /// </summary>
    public GumpFlags Flags { get; set; } = GumpFlags.Closable | GumpFlags.Dragable;
    
    /// <summary>
    /// All elements in this gump
    /// </summary>
    public List<GumpElement> Elements { get; } = new();
    
    /// <summary>
    /// Text pool referenced by elements
    /// </summary>
    public List<string> Texts { get; } = new();
    
    public void Serialize(PacketWriter writer)
    {
        writer.WriteUInt32(GumpTypeId);
        writer.WriteUInt32(Serial);
        writer.WriteInt16((short)X);
        writer.WriteInt16((short)Y);
        writer.WriteByte((byte)Flags);
        
        // Write elements
        writer.WriteUInt16((ushort)Elements.Count);
        foreach (var element in Elements)
        {
            element.Serialize(writer);
        }
        
        // Write text pool
        writer.WriteUInt16((ushort)Texts.Count);
        foreach (var text in Texts)
        {
            writer.WriteString(text);
        }
    }
    
    public static GumpData Deserialize(ref PacketReader reader)
    {
        var data = new GumpData
        {
            GumpTypeId = reader.ReadUInt32(),
            Serial = reader.ReadUInt32(),
            X = reader.ReadInt16(),
            Y = reader.ReadInt16(),
            Flags = (GumpFlags)reader.ReadByte()
        };
        
        // Read elements
        var elementCount = reader.ReadUInt16();
        for (int i = 0; i < elementCount; i++)
        {
            var element = GumpElement.Deserialize(ref reader);
            if (element != null)
            {
                data.Elements.Add(element);
            }
        }
        
        // Read text pool
        var textCount = reader.ReadUInt16();
        for (int i = 0; i < textCount; i++)
        {
            data.Texts.Add(reader.ReadString());
        }
        
        return data;
    }
}

/// <summary>
/// Response from client when interacting with a gump
/// </summary>
public class GumpResponse : ISerializable
{
    /// <summary>
    /// Type ID of the gump being responded to
    /// </summary>
    public uint GumpTypeId { get; set; }
    
    /// <summary>
    /// Serial of the gump instance
    /// </summary>
    public uint Serial { get; set; }
    
    /// <summary>
    /// Button that was clicked (0 = closed/cancelled)
    /// </summary>
    public int ButtonId { get; set; }
    
    /// <summary>
    /// IDs of checked checkboxes/selected radio buttons
    /// </summary>
    public List<int> Switches { get; } = new();
    
    /// <summary>
    /// Text entries (entryId -> text)
    /// </summary>
    public Dictionary<int, string> TextEntries { get; } = new();
    
    /// <summary>
    /// Check if a switch (checkbox/radio) is on
    /// </summary>
    public bool IsSwitchOn(int switchId) => Switches.Contains(switchId);
    
    /// <summary>
    /// Get text entry by ID
    /// </summary>
    public string? GetTextEntry(int entryId)
    {
        return TextEntries.TryGetValue(entryId, out var text) ? text : null;
    }
    
    public void Serialize(PacketWriter writer)
    {
        writer.WriteUInt32(GumpTypeId);
        writer.WriteUInt32(Serial);
        writer.WriteInt32(ButtonId);
        
        // Write switches
        writer.WriteUInt16((ushort)Switches.Count);
        foreach (var switchId in Switches)
        {
            writer.WriteInt32(switchId);
        }
        
        // Write text entries
        writer.WriteUInt16((ushort)TextEntries.Count);
        foreach (var kvp in TextEntries)
        {
            writer.WriteInt16((short)kvp.Key);
            writer.WriteString(kvp.Value);
        }
    }
    
    public static GumpResponse Deserialize(ref PacketReader reader)
    {
        var response = new GumpResponse
        {
            GumpTypeId = reader.ReadUInt32(),
            Serial = reader.ReadUInt32(),
            ButtonId = reader.ReadInt32()
        };
        
        // Read switches
        var switchCount = reader.ReadUInt16();
        for (int i = 0; i < switchCount; i++)
        {
            response.Switches.Add(reader.ReadInt32());
        }
        
        // Read text entries
        var textCount = reader.ReadUInt16();
        for (int i = 0; i < textCount; i++)
        {
            var entryId = reader.ReadInt16();
            var text = reader.ReadString();
            response.TextEntries[entryId] = text;
        }
        
        return response;
    }
}
