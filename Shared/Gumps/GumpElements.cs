using RealmOfReality.Shared.Serialization;

namespace RealmOfReality.Shared.Gumps;

/// <summary>
/// Types of gump elements
/// </summary>
public enum GumpElementType : byte
{
    Background = 0,      // Resizable 9-slice background
    Image = 1,           // Static image/graphic
    ImageTiled = 2,      // Tiled image fill
    Label = 3,           // Static text
    LabelCropped = 4,    // Text cropped to bounds
    Html = 5,            // HTML/rich text with optional scrollbar
    Button = 6,          // Clickable button
    TextEntry = 7,       // Editable text input
    Checkbox = 8,        // Toggle checkbox
    Radio = 9,           // Radio button (mutually exclusive in group)
    Item = 10,           // Game item display
    Tooltip = 11,        // Hover tooltip (attaches to previous element)
    AlphaRegion = 12,    // Transparent/semi-transparent region
    Group = 13,          // Group marker for radio buttons
    PaperdollBody = 14,  // Paperdoll body with equipment layers
}

/// <summary>
/// Button behavior types
/// </summary>
public enum GumpButtonType : byte
{
    Page = 0,    // Changes page without server response
    Reply = 1,   // Sends response to server
}

/// <summary>
/// Gump flags for behavior
/// </summary>
[Flags]
public enum GumpFlags : byte
{
    None = 0,
    Closable = 1 << 0,      // Can be closed by right-click or X
    Dragable = 1 << 1,      // Can be moved by dragging
    Disposable = 1 << 2,    // Auto-closes when moving away
    Resizable = 1 << 3,     // Can be resized
    Modal = 1 << 4,         // Blocks interaction with other gumps
}

/// <summary>
/// Base class for all gump elements
/// </summary>
public abstract class GumpElement : ISerializable
{
    public abstract GumpElementType Type { get; }
    public byte Page { get; set; } = 0;  // 0 = visible on all pages
    public int X { get; set; }
    public int Y { get; set; }
    
    public abstract void Serialize(PacketWriter writer);
    
    public static GumpElement? Deserialize(ref PacketReader reader)
    {
        var type = (GumpElementType)reader.ReadByte();
        var page = reader.ReadByte();
        var x = reader.ReadInt16();
        var y = reader.ReadInt16();
        
        GumpElement? element = type switch
        {
            GumpElementType.Background => new GumpBackground(),
            GumpElementType.Image => new GumpImage(),
            GumpElementType.ImageTiled => new GumpImageTiled(),
            GumpElementType.Label => new GumpLabel(),
            GumpElementType.LabelCropped => new GumpLabelCropped(),
            GumpElementType.Html => new GumpHtml(),
            GumpElementType.Button => new GumpButton(),
            GumpElementType.TextEntry => new GumpTextEntry(),
            GumpElementType.Checkbox => new GumpCheckbox(),
            GumpElementType.Radio => new GumpRadio(),
            GumpElementType.Item => new GumpItem(),
            GumpElementType.Tooltip => new GumpTooltip(),
            GumpElementType.AlphaRegion => new GumpAlphaRegion(),
            GumpElementType.Group => new GumpGroup(),
            GumpElementType.PaperdollBody => new GumpPaperdollBody(),
            _ => null
        };
        
        if (element != null)
        {
            element.Page = page;
            element.X = x;
            element.Y = y;
            element.DeserializeBody(ref reader);
        }
        
        return element;
    }
    
    protected abstract void DeserializeBody(ref PacketReader reader);
    
    protected void WriteHeader(PacketWriter writer)
    {
        writer.WriteByte((byte)Type);
        writer.WriteByte(Page);
        writer.WriteInt16((short)X);
        writer.WriteInt16((short)Y);
    }
}

/// <summary>
/// Resizable background using 9-slice scaling
/// </summary>
public class GumpBackground : GumpElement
{
    public override GumpElementType Type => GumpElementType.Background;
    public int Width { get; set; }
    public int Height { get; set; }
    public int GumpId { get; set; }  // Background graphic ID
    
    public override void Serialize(PacketWriter writer)
    {
        WriteHeader(writer);
        writer.WriteInt16((short)Width);
        writer.WriteInt16((short)Height);
        writer.WriteInt32(GumpId);
    }
    
    protected override void DeserializeBody(ref PacketReader reader)
    {
        Width = reader.ReadInt16();
        Height = reader.ReadInt16();
        GumpId = reader.ReadInt32();
    }
}

/// <summary>
/// Static image display
/// </summary>
public class GumpImage : GumpElement
{
    public override GumpElementType Type => GumpElementType.Image;
    public int GumpId { get; set; }
    public int Hue { get; set; }
    
    public override void Serialize(PacketWriter writer)
    {
        WriteHeader(writer);
        writer.WriteInt32(GumpId);
        writer.WriteInt16((short)Hue);
    }
    
    protected override void DeserializeBody(ref PacketReader reader)
    {
        GumpId = reader.ReadInt32();
        Hue = reader.ReadInt16();
    }
}

/// <summary>
/// Tiled image fill
/// </summary>
public class GumpImageTiled : GumpElement
{
    public override GumpElementType Type => GumpElementType.ImageTiled;
    public int Width { get; set; }
    public int Height { get; set; }
    public int GumpId { get; set; }
    
    public override void Serialize(PacketWriter writer)
    {
        WriteHeader(writer);
        writer.WriteInt16((short)Width);
        writer.WriteInt16((short)Height);
        writer.WriteInt32(GumpId);
    }
    
    protected override void DeserializeBody(ref PacketReader reader)
    {
        Width = reader.ReadInt16();
        Height = reader.ReadInt16();
        GumpId = reader.ReadInt32();
    }
}

/// <summary>
/// Static text label
/// </summary>
public class GumpLabel : GumpElement
{
    public override GumpElementType Type => GumpElementType.Label;
    public int Hue { get; set; }
    public int TextIndex { get; set; }  // Index into gump's text array
    
    public override void Serialize(PacketWriter writer)
    {
        WriteHeader(writer);
        writer.WriteInt16((short)Hue);
        writer.WriteInt16((short)TextIndex);
    }
    
    protected override void DeserializeBody(ref PacketReader reader)
    {
        Hue = reader.ReadInt16();
        TextIndex = reader.ReadInt16();
    }
}

/// <summary>
/// Text label cropped to bounds
/// </summary>
public class GumpLabelCropped : GumpElement
{
    public override GumpElementType Type => GumpElementType.LabelCropped;
    public int Width { get; set; }
    public int Height { get; set; }
    public int Hue { get; set; }
    public int TextIndex { get; set; }
    
    public override void Serialize(PacketWriter writer)
    {
        WriteHeader(writer);
        writer.WriteInt16((short)Width);
        writer.WriteInt16((short)Height);
        writer.WriteInt16((short)Hue);
        writer.WriteInt16((short)TextIndex);
    }
    
    protected override void DeserializeBody(ref PacketReader reader)
    {
        Width = reader.ReadInt16();
        Height = reader.ReadInt16();
        Hue = reader.ReadInt16();
        TextIndex = reader.ReadInt16();
    }
}

/// <summary>
/// HTML/rich text with optional background and scrollbar
/// </summary>
public class GumpHtml : GumpElement
{
    public override GumpElementType Type => GumpElementType.Html;
    public int Width { get; set; }
    public int Height { get; set; }
    public int TextIndex { get; set; }
    public bool HasBackground { get; set; }
    public bool HasScrollbar { get; set; }
    
    public override void Serialize(PacketWriter writer)
    {
        WriteHeader(writer);
        writer.WriteInt16((short)Width);
        writer.WriteInt16((short)Height);
        writer.WriteInt16((short)TextIndex);
        byte flags = 0;
        if (HasBackground) flags |= 1;
        if (HasScrollbar) flags |= 2;
        writer.WriteByte(flags);
    }
    
    protected override void DeserializeBody(ref PacketReader reader)
    {
        Width = reader.ReadInt16();
        Height = reader.ReadInt16();
        TextIndex = reader.ReadInt16();
        var flags = reader.ReadByte();
        HasBackground = (flags & 1) != 0;
        HasScrollbar = (flags & 2) != 0;
    }
}

/// <summary>
/// Clickable button
/// </summary>
public class GumpButton : GumpElement
{
    public override GumpElementType Type => GumpElementType.Button;
    public int NormalId { get; set; }      // Normal state graphic
    public int PressedId { get; set; }     // Pressed state graphic
    public GumpButtonType ButtonType { get; set; }
    public int Param { get; set; }         // Page number or custom param
    public int ButtonId { get; set; }      // Returned in response
    
    public override void Serialize(PacketWriter writer)
    {
        WriteHeader(writer);
        writer.WriteInt32(NormalId);
        writer.WriteInt32(PressedId);
        writer.WriteByte((byte)ButtonType);
        writer.WriteInt32(Param);
        writer.WriteInt32(ButtonId);
    }
    
    protected override void DeserializeBody(ref PacketReader reader)
    {
        NormalId = reader.ReadInt32();
        PressedId = reader.ReadInt32();
        ButtonType = (GumpButtonType)reader.ReadByte();
        Param = reader.ReadInt32();
        ButtonId = reader.ReadInt32();
    }
}

/// <summary>
/// Editable text input field
/// </summary>
public class GumpTextEntry : GumpElement
{
    public override GumpElementType Type => GumpElementType.TextEntry;
    public int Width { get; set; }
    public int Height { get; set; }
    public int Hue { get; set; }
    public int EntryId { get; set; }       // ID returned in response
    public int InitialTextIndex { get; set; }  // Index to initial text
    public int MaxLength { get; set; }
    
    public override void Serialize(PacketWriter writer)
    {
        WriteHeader(writer);
        writer.WriteInt16((short)Width);
        writer.WriteInt16((short)Height);
        writer.WriteInt16((short)Hue);
        writer.WriteInt16((short)EntryId);
        writer.WriteInt16((short)InitialTextIndex);
        writer.WriteInt16((short)MaxLength);
    }
    
    protected override void DeserializeBody(ref PacketReader reader)
    {
        Width = reader.ReadInt16();
        Height = reader.ReadInt16();
        Hue = reader.ReadInt16();
        EntryId = reader.ReadInt16();
        InitialTextIndex = reader.ReadInt16();
        MaxLength = reader.ReadInt16();
    }
}

/// <summary>
/// Checkbox toggle
/// </summary>
public class GumpCheckbox : GumpElement
{
    public override GumpElementType Type => GumpElementType.Checkbox;
    public int UncheckedId { get; set; }
    public int CheckedId { get; set; }
    public int SwitchId { get; set; }      // ID returned when checked
    public bool InitialState { get; set; }
    
    public override void Serialize(PacketWriter writer)
    {
        WriteHeader(writer);
        writer.WriteInt32(UncheckedId);
        writer.WriteInt32(CheckedId);
        writer.WriteInt32(SwitchId);
        writer.WriteBool(InitialState);
    }
    
    protected override void DeserializeBody(ref PacketReader reader)
    {
        UncheckedId = reader.ReadInt32();
        CheckedId = reader.ReadInt32();
        SwitchId = reader.ReadInt32();
        InitialState = reader.ReadBool();
    }
}

/// <summary>
/// Radio button (mutually exclusive within group)
/// </summary>
public class GumpRadio : GumpElement
{
    public override GumpElementType Type => GumpElementType.Radio;
    public int UncheckedId { get; set; }
    public int CheckedId { get; set; }
    public int SwitchId { get; set; }
    public int GroupId { get; set; }
    public bool InitialState { get; set; }
    
    public override void Serialize(PacketWriter writer)
    {
        WriteHeader(writer);
        writer.WriteInt32(UncheckedId);
        writer.WriteInt32(CheckedId);
        writer.WriteInt32(SwitchId);
        writer.WriteInt32(GroupId);
        writer.WriteBool(InitialState);
    }
    
    protected override void DeserializeBody(ref PacketReader reader)
    {
        UncheckedId = reader.ReadInt32();
        CheckedId = reader.ReadInt32();
        SwitchId = reader.ReadInt32();
        GroupId = reader.ReadInt32();
        InitialState = reader.ReadBool();
    }
}

/// <summary>
/// Game item display (uses item art, not gump art)
/// </summary>
public class GumpItem : GumpElement
{
    public override GumpElementType Type => GumpElementType.Item;
    public int ItemId { get; set; }
    public int Hue { get; set; }
    
    public override void Serialize(PacketWriter writer)
    {
        WriteHeader(writer);
        writer.WriteInt32(ItemId);
        writer.WriteInt16((short)Hue);
    }
    
    protected override void DeserializeBody(ref PacketReader reader)
    {
        ItemId = reader.ReadInt32();
        Hue = reader.ReadInt16();
    }
}

/// <summary>
/// Tooltip that attaches to the previous element
/// </summary>
public class GumpTooltip : GumpElement
{
    public override GumpElementType Type => GumpElementType.Tooltip;
    public int TextIndex { get; set; }
    
    public override void Serialize(PacketWriter writer)
    {
        WriteHeader(writer);
        writer.WriteInt16((short)TextIndex);
    }
    
    protected override void DeserializeBody(ref PacketReader reader)
    {
        TextIndex = reader.ReadInt16();
    }
}

/// <summary>
/// Transparent/semi-transparent region
/// </summary>
public class GumpAlphaRegion : GumpElement
{
    public override GumpElementType Type => GumpElementType.AlphaRegion;
    public int Width { get; set; }
    public int Height { get; set; }
    
    public override void Serialize(PacketWriter writer)
    {
        WriteHeader(writer);
        writer.WriteInt16((short)Width);
        writer.WriteInt16((short)Height);
    }
    
    protected override void DeserializeBody(ref PacketReader reader)
    {
        Width = reader.ReadInt16();
        Height = reader.ReadInt16();
    }
}

/// <summary>
/// Group marker for radio buttons
/// </summary>
public class GumpGroup : GumpElement
{
    public override GumpElementType Type => GumpElementType.Group;
    public int GroupId { get; set; }
    
    public override void Serialize(PacketWriter writer)
    {
        WriteHeader(writer);
        writer.WriteInt32(GroupId);
    }
    
    protected override void DeserializeBody(ref PacketReader reader)
    {
        GroupId = reader.ReadInt32();
    }
}

/// <summary>
/// Paperdoll body with equipment layers
/// Used to render character in paperdoll gump
/// </summary>
public class GumpPaperdollBody : GumpElement
{
    public override GumpElementType Type => GumpElementType.PaperdollBody;
    
    // Body info
    public ushort BodyId { get; set; }      // Body graphic (400=male, 401=female)
    public byte Gender { get; set; }         // 0=male, 1=female
    public ushort SkinHue { get; set; }      // Skin color
    
    // Hair
    public ushort HairStyle { get; set; }    // Hair graphic ID
    public ushort HairHue { get; set; }      // Hair color
    
    // Facial hair (male only)
    public ushort BeardStyle { get; set; }   // Beard graphic ID  
    public ushort BeardHue { get; set; }     // Beard color
    
    // Equipment layers (layer -> (itemId, gumpId, hue))
    public List<PaperdollLayer> Layers { get; } = new();
    
    public override void Serialize(PacketWriter writer)
    {
        WriteHeader(writer);
        
        writer.WriteUInt16(BodyId);
        writer.WriteByte(Gender);
        writer.WriteUInt16(SkinHue);
        writer.WriteUInt16(HairStyle);
        writer.WriteUInt16(HairHue);
        writer.WriteUInt16(BeardStyle);
        writer.WriteUInt16(BeardHue);
        
        // Write equipment layers
        writer.WriteByte((byte)Layers.Count);
        foreach (var layer in Layers)
        {
            writer.WriteByte(layer.Layer);
            writer.WriteUInt64(layer.Serial);
            writer.WriteUInt16(layer.ItemId);
            writer.WriteUInt16(layer.GumpId);
            writer.WriteUInt16(layer.Hue);
        }
    }
    
    protected override void DeserializeBody(ref PacketReader reader)
    {
        BodyId = reader.ReadUInt16();
        Gender = reader.ReadByte();
        SkinHue = reader.ReadUInt16();
        HairStyle = reader.ReadUInt16();
        HairHue = reader.ReadUInt16();
        BeardStyle = reader.ReadUInt16();
        BeardHue = reader.ReadUInt16();
        
        var layerCount = reader.ReadByte();
        Layers.Clear();
        for (int i = 0; i < layerCount; i++)
        {
            Layers.Add(new PaperdollLayer
            {
                Layer = reader.ReadByte(),
                Serial = reader.ReadUInt64(),
                ItemId = reader.ReadUInt16(),
                GumpId = reader.ReadUInt16(),
                Hue = reader.ReadUInt16()
            });
        }
    }
}

/// <summary>
/// Equipment layer data for paperdoll
/// </summary>
public class PaperdollLayer
{
    public byte Layer { get; set; }      // Layer enum value
    public ulong Serial { get; set; }    // Item serial for interaction
    public ushort ItemId { get; set; }   // Item graphic
    public ushort GumpId { get; set; }   // Paperdoll gump graphic
    public ushort Hue { get; set; }      // Item hue
}
