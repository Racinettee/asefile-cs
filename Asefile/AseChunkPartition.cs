using System.Drawing;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace Asefile;

internal enum AseChunkType
{
    OldPalette1 = 0x0004,
    OldPalette2 = 0x0011,
    Layer = 0x2004,
    Cel = 0x2005,
    CelExtra = 0x2006,
    ColorProfile = 0x2007,
    ExtraFiles = 0x2008,
    Mask = 0x2016, // deprecated
    Path = 0x2017, // never used
    Tags = 0x2018,
    Palette = 0x2019,
    UserData = 0x2020,
    Slice = 0x2022,
    Tileset = 0x2023,
}

/*
 * DWORD       Chunk size
   WORD        Chunk type
   BYTE[]      Chunk data
   
   Note: The chunk size includes the DWORD of the size itself, and the WORD of the chunk
   type, so a chunk size must be equal or greater than 6 bytes at least.
 */
public class AseChunkPartition
{
    public virtual UserDataChunkPartition? UserData { get; set; }
    
    protected virtual string DecodeString(BinaryReader reader)
    {
        ushort strLenBytes = reader.ReadUInt16();
        byte[] buffer = new byte[strLenBytes];
        reader.Read(buffer);
        return Encoding.UTF8.GetString(buffer, 0, strLenBytes);
    }

    protected virtual void EncodeString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.Write((ushort)bytes.Length);
        writer.Write(bytes);
    }
}

/*
Layer Chunk (0x2004)
   
   In the first frame should be a set of layer chunks to determine the entire layers layout:
   
   WORD        Flags:
                 1 = Visible
                 2 = Editable
                 4 = Lock movement
                 8 = Background
                 16 = Prefer linked cels
                 32 = The layer group should be displayed collapsed
                 64 = The layer is a reference layer
   WORD        Layer type
                 0 = Normal (image) layer
                 1 = Group
                 2 = Tilemap
   WORD        Layer child level (see NOTE.1)
   WORD        Default layer width in pixels (ignored)
   WORD        Default layer height in pixels (ignored)
   WORD        Blend mode (always 0 for layer set)
                 Normal         = 0
                 Multiply       = 1
                 Screen         = 2
                 Overlay        = 3
                 Darken         = 4
                 Lighten        = 5
                 Color Dodge    = 6
                 Color Burn     = 7
                 Hard Light     = 8
                 Soft Light     = 9
                 Difference     = 10
                 Exclusion      = 11
                 Hue            = 12
                 Saturation     = 13
                 Color          = 14
                 Luminosity     = 15
                 Addition       = 16
                 Subtract       = 17
                 Divide         = 18
   BYTE        Opacity
                 Note: valid only if file header flags field has bit 1 set
   BYTE[3]     For future (set to zero)
   STRING      Layer name
   + If layer type = 2
     DWORD     Tileset index
*/
public class LayerChunkPartition : AseChunkPartition
{
    public virtual ushort Flags { get; set; }
    public virtual ushort LayerType { get; set; }
    public virtual ushort LayerChildLevel { get; set; } // see note 1
    public virtual ushort DefaultLayerWidthInPixels { get; set; }
    public virtual ushort DefaultLayerHeightInPixels { get; set; }
    public virtual ushort BlendMode { get; set; }
    public virtual byte   Opacity { get; set; }
    public virtual string LayerName { get; set; }
    public virtual uint   TilesetIndex { get; set; } // if layertype == 2

    public LayerChunkPartition()
    {
    }

    public LayerChunkPartition(BinaryReader reader)
    {
        Decode(reader);
    }

    public void Encode(BinaryWriter writer)
    {
        writer.Write(Flags);
        writer.Write(LayerType);
        writer.Write(LayerChildLevel);
        writer.Write(DefaultLayerWidthInPixels);
        writer.Write(DefaultLayerHeightInPixels);
        writer.Write(BlendMode);
        writer.Write(Opacity);
        writer.Write(new byte[3]); // for future use
        EncodeString(writer, LayerName);
        if (LayerType == 2)
            writer.Write(TilesetIndex);
    }

    public void Decode(BinaryReader reader)
    {
        Flags = reader.ReadUInt16();
        LayerType = reader.ReadUInt16();
        LayerChildLevel = reader.ReadUInt16();
        DefaultLayerWidthInPixels = reader.ReadUInt16();
        DefaultLayerHeightInPixels = reader.ReadUInt16();
        BlendMode = reader.ReadUInt16();
        Opacity = reader.ReadByte();
        reader.Read(new byte[3]); // for future use
        LayerName = DecodeString(reader);
        if (LayerType == 2)
            TilesetIndex = reader.ReadUInt32();
    }
}

/// <summary>
/// This chunk determine where to put a cel in the specified layer/frame.
///
/// WORD        Layer index (see NOTE.2)
/// SHORT       X position
/// SHORT       Y position
/// BYTE        Opacity level
/// WORD        Cel Type
/// 0 - Raw Image Data (unused, compressed image is preferred)
/// 1 - Linked Cel
/// 2 - Compressed Image
/// 3 - Compressed Tilemap
/// SHORT       Z-Index (see NOTE.5)
/// 0 = default layer ordering
/// +N = show this cel N layers later
/// -N = show this cel N layers back
/// BYTE[5]     For future (set to zero)
/// + For cel type = 0 (Raw Image Data)
/// WORD      Width in pixels
/// WORD      Height in pixels
/// PIXEL[]   Raw pixel data: row by row from top to bottom,
/// for each scanline read pixels from left to right.
/// + For cel type = 1 (Linked Cel)
/// WORD      Frame position to link with
/// + For cel type = 2 (Compressed Image)
/// WORD      Width in pixels
/// WORD      Height in pixels
/// PIXEL[]   "Raw Cel" data compressed with ZLIB method (see NOTE.3)
/// + For cel type = 3 (Compressed Tilemap)
/// WORD      Width in number of tiles
/// WORD      Height in number of tiles
/// WORD      Bits per tile (at the moment it's always 32-bit per tile)
/// DWORD     Bitmask for tile ID (e.g. 0x1fffffff for 32-bit tiles)
/// DWORD     Bitmask for X flip
/// DWORD     Bitmask for Y flip
/// DWORD     Bitmask for diagonal flip (swap X/Y axis)
/// BYTE[10]  Reserved
/// TILE[]    Row by row, from top to bottom tile by tile
/// compressed with ZLIB method (see NOTE.3)
/// </summary>
public class CelChunkPartition : AseChunkPartition
{
    public virtual ushort LayerIndex { get; set; } // see note 2
    public virtual short  XPos { get; set; }
    public virtual short  YPos { get; set; }
    public virtual byte   OpacityLevel { get; set; }
    public virtual ushort CelType { get; set; }
    public virtual short  ZIndex { get; set; } // see note 5
    public virtual ushort WidthInPixels { get; set; }
    public virtual ushort HeightInPixels { get; set; }
    public virtual Color[] PixelData { get; set; } // // if cel type = 2 then this is the decompressed zlib data
    public virtual ushort FramePosToLink { get; set; }
    public virtual ushort WidthInTiles { get; set; }
    public virtual ushort HeightInTiles { get; set; }
    public virtual ushort BitsPerTile { get; set; } // currently always 32 bits
    public virtual uint   BitmaskOfTileId { get; set; }
    public virtual uint   BitmaskForXFlip { get; set; }
    public virtual uint   BitmaskForYFlip { get; set; }
    public virtual uint   BitmaskForDiagFlip { get; set; }
    public virtual byte[] TileData { get; set; } // always zlib compressed

    public virtual CelExtraChunkPartition? Extra { get; set; }= null;

    private int chunkSize = 0;
    private AseHeader header;

    public CelChunkPartition()
    {
    }

    public CelChunkPartition(BinaryReader reader, AseHeader header, int chunkSize)
    {
        this.header = header;
        this.chunkSize = chunkSize;
        Decode(reader);
    }

    public void Encode(BinaryWriter writer)
    {
        writer.Write(LayerIndex);
        writer.Write(XPos);
        writer.Write(YPos);
        writer.Write(OpacityLevel);
        writer.Write(CelType);
        writer.Write(ZIndex);
        writer.Write(new byte[5]); // for future!

        switch (CelType)
        {
            case 0: // raw image data
                writer.Write(WidthInPixels);
                writer.Write(HeightInPixels);
                //writer.Write(PixelData); // TODO: convert this back to bytes and then write
                break;

            case 1:
                writer.Write(FramePosToLink);
                break;

            case 2:
                writer.Write(WidthInPixels);
                writer.Write(HeightInPixels);
                //writer.Write(CompressedCelData); // TODO: compress pixel data using zlib method - NOTE 3
                break;

            case 3:
                writer.Write(WidthInTiles);
                writer.Write(HeightInTiles);
                writer.Write(BitsPerTile);
                writer.Write(BitmaskOfTileId);
                writer.Write(BitmaskForXFlip);
                writer.Write(BitmaskForYFlip);
                writer.Write(BitmaskForDiagFlip);
                writer.Write(new byte[10]); // reserved
                writer.Write(TileData);
                break;
        }
    }

    public void Decode(BinaryReader reader)
    {
        LayerIndex   = reader.ReadUInt16();
        XPos         = reader.ReadInt16();
        YPos         = reader.ReadInt16();
        OpacityLevel = reader.ReadByte();
        CelType      = reader.ReadUInt16();
        ZIndex       = reader.ReadInt16();
        reader.Read(new byte[5]); // for future!

        switch (CelType)
        {
            case 0: // raw image data
                WidthInPixels  = reader.ReadUInt16();
                HeightInPixels = reader.ReadUInt16();
                PixelData = DecodePixels(reader, bytesToRead: WidthInPixels * HeightInPixels);
                break; 
            case 1:
                FramePosToLink = reader.ReadUInt16();
                break;
            case 2:
                WidthInPixels = reader.ReadUInt16();
                HeightInPixels = reader.ReadUInt16();
                // the amount of data to read is the chunk size minus the size of all the data preceding this sub-chunk (26 bytes total)
                const int SizeChunksUpToData = 26;
                int readDataSize = chunkSize - SizeChunksUpToData;
                var compressedCelData = new byte[readDataSize];
                reader.Read(compressedCelData); // NOTE 3
                using (MemoryStream compressedStream = new MemoryStream(compressedCelData))
                {
                    using var deflateStream = new ZLibStream(compressedStream, CompressionMode.Decompress);
                    using var outputStream = new MemoryStream();
                    deflateStream.CopyTo(outputStream);
                    outputStream.Position = 0; // need to reset this or binary stream will start from the end
                    using var binaryStream = new BinaryReader(outputStream);
                    PixelData = DecodePixels(binaryStream, bytesToRead: WidthInPixels * HeightInPixels);
                }
                break;
            case 3:
                WidthInTiles = reader.ReadUInt16();
                HeightInTiles = reader.ReadUInt16();
                BitsPerTile = reader.ReadUInt16();
                BitmaskOfTileId = reader.ReadUInt32();
                BitmaskForXFlip = reader.ReadUInt32();
                BitmaskForYFlip = reader.ReadUInt32();
                BitmaskForDiagFlip = reader.ReadUInt32();
                reader.Read(new byte[10]); // reserved
                TileData = new byte[WidthInPixels * HeightInPixels]; // TODO: come back to this 
                break;
        }
    }

    private Color[] DecodePixels(BinaryReader reader, int bytesToRead)
    {
        Color[] result;
        switch (header.ColorDepth)
        {
            case 32: // 32bit rgba
                bytesToRead *= 4;
                result = new Color[bytesToRead / 4];
                for (int i = 0; i < result.Length; i++)
                {
                    var r = reader.ReadByte();
                    var g = reader.ReadByte();
                    var b = reader.ReadByte();
                    var a32 = reader.ReadByte();
                    result[i] = Color.FromArgb(a32, r, g, b);
                }
                break;
            case 16: // 16bit grayscale
                bytesToRead *= 2;
                result = new Color[bytesToRead / 2];
                for (int i = 0; i < result.Length; i++)
                {
                    var v = reader.ReadByte();
                    var a16 = reader.ReadByte();
                    result[i] = Color.FromArgb(a16, v, v, v);    
                }
                break;
            default: // indexed color, just an index into a palette
                result = new Color[bytesToRead];
                for (int i = 0; i < result.Length; i++)
                {
                    var idx = reader.ReadByte();
                    result[i] = Color.FromArgb(idx, idx, idx, idx);
                }
                break;
        }

        return result;
    }
}

/// <summary>
/// Cel Extra Chunk: 0x2006
/// </summary>
public class CelExtraChunkPartition : AseChunkPartition
{
    public virtual uint Flags { get; set; }
    public virtual float PreciseXPos { get; set; }
    public virtual float PreciseYPos { get; set; }
    public virtual float WidthOfCelInSprite { get; set; }
    public virtual float HeightOfCelInSprite { get; set; }

    public CelExtraChunkPartition()
    {
    }

    public CelExtraChunkPartition(BinaryReader reader)
    {
        Decode(reader);
    }

    public void Decode(BinaryReader reader)
    {
        Flags = reader.ReadUInt32();
        PreciseXPos = reader.ReadSingle();
        PreciseYPos = reader.ReadSingle();
        WidthOfCelInSprite = reader.ReadSingle();
        HeightOfCelInSprite = reader.ReadSingle();
        reader.Read(new byte[16]); // 16 reserved bytes
    }
}

/// <summary>
/// Color Profile Chunk: 0x2007
/// </summary>
public class ColorProfileChunkPartition : AseChunkPartition
{
    public virtual ushort Type { get; set; } // 0: no color profile, 1: use srgb, 2: use embeded icc profile
    public virtual ushort Flags { get; set; }
    public virtual float  Gamma { get; set; }

    // + if type == icc
    public virtual uint IccProfileDataLen { get; set; }
    public virtual byte[]? IccProfile { get; set; }

    public ColorProfileChunkPartition()
    {
    }

    public ColorProfileChunkPartition(BinaryReader reader)
    {
        Decode(reader);
    }

    public void Decode(BinaryReader reader)
    {
        Type = reader.ReadUInt16();
        Flags = reader.ReadUInt16();
        Gamma = reader.ReadSingle();
        reader.Read(new byte[8]); // reserved, set 0
        if (Type == 2) // ICC
        {
            IccProfileDataLen = reader.ReadUInt32();
            IccProfile = reader.ReadBytes((int)IccProfileDataLen);
        }
    }
}

/// <summary>
/// External Files Chunk (0x2008)
/// A list of external files linked with this file can be found in the first frame. It might be used to reference external palettes, tilesets, or extensions that make use of extended properties.
/// DWORD       Number of entries
/// BYTE[8]     Reserved (set to zero)
///   + For each entry
///   DWORD     Entry ID (this ID is referenced by tilesets, palettes, or extended properties)
///   BYTE      Type
///              0 - External palette
///              1 - External tileset
///              2 - Extension name for properties
///              3 - Extension name for tile management (can exist one per sprite)
///              BYTE[7]   Reserved (set to zero)
///              STRING    External file name or extension ID (see NOTE.4)
/// </summary>
public class ExternalFileChunkPartition : AseChunkPartition
{
    public virtual uint NumEntries { get; set; }
    private byte[] Reserved1 { get; set; }
    public virtual List<ExternalFileEntry> ExternalFileEntries { get; set; }
}

public class ExternalFileEntry
{
    public virtual uint EntryID { get; set; }
    public virtual byte Type { get; set; }
    private byte[] Reserved1 { get; set; }
    private string ExternalFileNameOrExtID { get; set; }
}

/// <summary>
/// Tags Chunk (0x2018)
/// 
/// After the tags chunk, you can write one user data chunk for each tag. E.g. if there are 10 tags, you can then write 10 user data chunks one for each tag.
/// 
/// WORD        Number of tags
/// BYTE[8]     For future (set to zero)
/// + For each tag
///   WORD      From frame
///   WORD      To frame
///   BYTE      Loop animation direction
///               0 = Forward
///               1 = Reverse
///               2 = Ping-pong
///               3 = Ping-pong Reverse
///   WORD      Repeat N times. Play this animation section N times:
///               0 = Doesn't specify (plays infinite in UI, once on export,
///                   for ping-pong it plays once in each direction)
///               1 = Plays once (for ping-pong, it plays just in one direction)
///               2 = Plays twice (for ping-pong, it plays once in one direction,
///                   and once in reverse)
///               n = Plays N times
///   BYTE[6]   For future (set to zero)
///   BYTE[3]   RGB values of the tag color
///               Deprecated, used only for backward compatibility with Aseprite v1.2.x
///               The color of the tag is the one in the user data field following
///               the tags chunk
///   BYTE      Extra byte (zero)
///   STRING    Tag name
/// </summary>
public class TagChunkPartition : AseChunkPartition
{
    public List<Tag> Tags { get; set; } = new();

    public TagChunkPartition()
    {
    }

    public TagChunkPartition(BinaryReader reader)
    {
        Decode(reader);
    }

    public void Decode(BinaryReader reader)
    {
        var numberOfTags = reader.ReadUInt16();
        reader.Read(new byte[8]);

        for (var i = 0; i < numberOfTags; i++)
        {
            Tags.Add(new Tag(reader));
        }
    }
}

public class Tag : AseChunkPartition
{
    public virtual ushort FromFrame { get; set; }
    public virtual ushort ToFrame { get; set; }
    public virtual byte   LoopDirection { get; set; }
    public virtual ushort RepeatTimes { get; set; }
    public virtual string TagName { get; set; }

    public Tag()
    {
    }

    public Tag(BinaryReader reader)
    {
        Decode(reader);
    }

    public void Decode(BinaryReader reader)
    {
        FromFrame = reader.ReadUInt16();
        ToFrame = reader.ReadUInt16();
        LoopDirection = reader.ReadByte();
        RepeatTimes = reader.ReadUInt16();
        reader.Read(new byte[6]); // reserved for future
        reader.Read(new byte[3]); // deprecated rgb value of tag color
        reader.ReadByte(); // extra byte
        TagName = DecodeString(reader);
    }
}

/// <summary>
/// Palette Chunk (0x2019)
/// 
/// DWORD       New palette size (total number of entries)
/// DWORD       First color index to change
/// DWORD       Last color index to change
/// BYTE[8]     For future (set to zero)
/// + For each palette entry in [from,to] range (to-from+1 entries)
///   WORD      Entry flags:
///               1 = Has name
///   BYTE      Red (0-255)
///   BYTE      Green (0-255)
///   BYTE      Blue (0-255)
///   BYTE      Alpha (0-255)
///   + If has name bit in entry flags
///     STRING  Color name
/// </summary>
public class PaletteChunkPartition : AseChunkPartition
{
    public virtual uint NewPaletteSize { get; set; }
    public virtual uint FirstColorIndexToChange { get; set; }
    public virtual uint LastColorIndexToChange { get; set; }
    // 8 bytes for the future
    public virtual List<PaletteChunkEntry> PaletteChunkEntries { get; set; } = new();

    public PaletteChunkPartition()
    {
    }

    public PaletteChunkPartition(BinaryReader reader)
    {
        NewPaletteSize = reader.ReadUInt32();
        FirstColorIndexToChange = reader.ReadUInt32();
        LastColorIndexToChange = reader.ReadUInt32();
        reader.Read(new byte[8]); // for future

        for (var i = 0; i < NewPaletteSize; i++)
        {
            PaletteChunkEntries.Add(new PaletteChunkEntry(reader));
        }
    }
}

public class PaletteChunkEntry : AseChunkPartition
{
    public virtual ushort EntryFlags { get; set; }
    public virtual byte Red { get; set; }
    public virtual byte Green { get; set; }
    public virtual byte Blue { get; set; }
    public virtual byte Alpha { get; set; }
    public virtual string ColorName { get; set; } = "";

    public PaletteChunkEntry()
    {
    }

    public PaletteChunkEntry(BinaryReader reader)
    {
        Decode(reader);
    }

    public void Decode(BinaryReader reader)
    {
        EntryFlags = reader.ReadUInt16();
        Red = reader.ReadByte();
        Green = reader.ReadByte();
        Blue = reader.ReadByte();
        Alpha = reader.ReadByte();

        if ((EntryFlags & 0x01) == 1)
            ColorName = DecodeString(reader);
    }
}

/* <summary>
User Data Chunk (0x2020)
   
   Specifies the user data (color/text/properties) to be associated with the last read chunk/object. E.g. If the last chunk we've read is a layer and then this chunk appears, this user data belongs to that layer, if we've read a cel, it belongs to that cel, etc. There are some special cases:
   
   After a Tags chunk, there will be several user data chunks, one for each tag, you should associate the user data in the same order as the tags are in the Tags chunk.
   After the Tileset chunk, it could be followed by a user data chunk (empty or not) and then all the user data chunks of the tiles ordered by tile index, or it could be followed by none user data chunk (if the file was created in an older Aseprite version of if no tile has user data).
   In Aseprite v1.3 a sprite has associated user data, to consider this case there is an User Data Chunk at the first frame after the Palette Chunk.
   
   The data of this chunk is as follows:
   
   DWORD       Flags
   1 = Has text
   2 = Has color
   4 = Has properties
   + If flags have bit 1
   STRING    Text
   + If flags have bit 2
   BYTE      Color Red (0-255)
   BYTE      Color Green (0-255)
   BYTE      Color Blue (0-255)
   BYTE      Color Alpha (0-255)
   + If flags have bit 4
   DWORD     Size in bytes of all properties maps stored in this chunk
   The size includes the this field and the number of property maps
   (so it will be a value greater or equal to 8 bytes).
   DWORD     Number of properties maps
   + For each properties map:
   DWORD     Properties maps key
   == 0 means user properties
   != 0 means an extension Entry ID (see External Files Chunk))
   DWORD     Number of properties
   + For each property:
   STRING    Name
   WORD      Type
   + If type==0x0001 (bool)
   BYTE    == 0 means FALSE
   != 0 means TRUE
   + If type==0x0002 (int8)
   BYTE
   + If type==0x0003 (uint8)
   BYTE
   + If type==0x0004 (int16)
   SHORT
   + If type==0x0005 (uint16)
   WORD
   + If type==0x0006 (int32)
   LONG
   + If type==0x0007 (uint32)
   DWORD
   + If type==0x0008 (int64)
   LONG64
   + If type==0x0009 (uint64)
   QWORD
   + If type==0x000A
   FIXED
   + If type==0x000B
   FLOAT
   + If type==0x000C
   DOUBLE
   + If type==0x000D
   STRING
   + If type==0x000E
   POINT
   + If type==0x000F
   SIZE
   + If type==0x0010
   RECT
   + If type==0x0011 (vector)
   DWORD     Number of elements
   WORD      Element's type.
   + If Element's type == 0 (all elements are not of the same type)
   For each element:
   WORD      Element's type
   BYTE[]    Element's value. Structure depends on the
   element's type
   + Else (all elements are of the same type)
   For each element:
   BYTE[]    Element's value. Structure depends on the
   element's type
   + If type==0x0012 (nested properties map)
   DWORD     Number of properties
   BYTE[]    Nested properties data
   Structure is the same as indicated in this loop
   + If type==0x0013
   UUID
   </summary> */
public class UserDataChunkPartition : AseChunkPartition
{
    public virtual uint Flags { get; set; }
    public virtual string Text { get; set; }
    public virtual Color Color { get; set; }
    public virtual Dictionary<string, object> UserProperties { get; set; } = new();

    public UserDataChunkPartition()
    {
    }

    public UserDataChunkPartition(BinaryReader reader)
    {
        Decode(reader);
    }

    public void Decode(BinaryReader reader)
    {
        Flags = reader.ReadUInt32();

        if ((Flags & 0x01) == 0x01)
            Text = DecodeString(reader);
        
        if ((Flags & 0x02) == 0x02)
        {
            var r = reader.ReadByte();
            var g = reader.ReadByte();
            var b = reader.ReadByte();
            var a = reader.ReadByte();
            Color = Color.FromArgb(a, r, g, b);
        }

        if ((Flags & 0x04) == 0x04)
        {
            var sizeInBytesOfAllPropertiesMapsStoredInThisChunk = reader.ReadUInt32();
            var numberOfPropertiesMaps = reader.ReadUInt32();

            // foreach properties map
            for (var i = 0; i < numberOfPropertiesMaps; i++)
            {
                var propertiesMapsKey = reader.ReadUInt32(); // == 0 -> user properties, != 0 -> extension entry id
                var numberOfProperties = reader.ReadUInt32();

                for (var j = 0; j < numberOfProperties; j++)
                {
                    var name = DecodeString(reader);
                    var type = reader.ReadUInt16();
                    
                    UserProperties.Add(name,
                        type switch
                        {
                            0x0001 => reader.ReadByte() != 0,
                            0x0002 => reader.ReadSByte(),
                            0x0003 => reader.ReadByte(),
                            0x0004 => reader.ReadInt16(),
                            0x0005 => reader.ReadUInt16(),
                            0x0006 => reader.ReadInt32(),
                            0x0007 => reader.ReadUInt32(),
                            0x0008 => reader.ReadInt64(),
                            0x0009 => reader.ReadUInt64(),
                            0x000A => reader.ReadSingle(),
                            0x000B => reader.ReadSingle(),
                            0x000C => reader.ReadDouble(),
                            0x000D => DecodeString(reader),
                            0x000E => DecodePoint(reader),
                            0x000F => DecodeSize(reader),
                            0x0010 => new Rectangle(DecodePoint(reader), DecodeSize(reader)),
                            0x0011 => DecodeVector(reader),
                            0x0012 => DecodePropertiesMap(reader),
                            0x0013 => DecodeUUID(reader),
                        });
                }
            }
        }
    }

    Point DecodePoint(BinaryReader reader) => new(reader.ReadInt32(), reader.ReadInt32());
    Size DecodeSize(BinaryReader reader) => new(reader.ReadInt32(), reader.ReadInt32());

    List<object> DecodeVector(BinaryReader reader)
    {
        var numElementsInVector = reader.ReadUInt32();
        var elementType = reader.ReadUInt16();
        var result = new List<object>((int)numElementsInVector); // reserve the known number of elements

        for (var i = 0; i < numElementsInVector; i++)
        {
            if (elementType == 0)
            {
                // We have to decode each elements type
                var thisElementType = reader.ReadUInt16();
                result.Add(DecodeProperty(thisElementType, reader));
            }
            else // all elements are the same
            {
                result.Add(DecodeProperty(elementType, reader));
            }
        }

        return result;
    }
    Guid DecodeUUID(BinaryReader reader)
    {
        const int aseFileUUIDByteCount = 16;
        var guidBytes = reader.ReadBytes(aseFileUUIDByteCount);
        return Guid.Parse(Encoding.ASCII.GetString(guidBytes));
    }

    Dictionary<string, object> DecodePropertiesMap(BinaryReader reader)
    {
        var result = new Dictionary<string, object>();
        
        var numberOfProperties = reader.ReadUInt32();

        for (var j = 0; j < numberOfProperties; j++)
        {
            var name = DecodeString(reader);
            var type = reader.ReadUInt16();
                    
            result.Add(name, DecodeProperty(type, reader));
        }

        return result;
    }

    protected virtual object DecodeProperty(ushort type, BinaryReader reader)
    {
        return type switch
        {
            0x0001 => reader.ReadByte() != 0,
            0x0002 => reader.ReadSByte(),
            0x0003 => reader.ReadByte(),
            0x0004 => reader.ReadInt16(),
            0x0005 => reader.ReadUInt16(),
            0x0006 => reader.ReadInt32(),
            0x0007 => reader.ReadUInt32(),
            0x0008 => reader.ReadInt64(),
            0x0009 => reader.ReadUInt64(),
            0x000A => reader.ReadSingle(),
            0x000B => reader.ReadSingle(),
            0x000C => reader.ReadDouble(),
            0x000D => DecodeString(reader),
            0x000E => DecodePoint(reader),
            0x000F => DecodeSize(reader),
            0x0010 => new Rectangle(DecodePoint(reader), DecodeSize(reader)),
            0x0011 => DecodeVector(reader),
            0x0012 => DecodePropertiesMap(reader),
            0x0013 => DecodeUUID(reader),
        };
    }
}

/// <summary>
/// Slice Chunk (0x2022)
/// 
/// DWORD       Number of "slice keys"
/// DWORD       Flags
///               1 = It's a 9-patches slice
///               2 = Has pivot information
/// DWORD       Reserved
/// STRING      Name
/// + For each slice key
///   DWORD     Frame number (this slice is valid from this frame to the end of the animation)
///   LONG      Slice X origin coordinate in the sprite
///   LONG      Slice Y origin coordinate in the sprite
///   DWORD     Slice width (can be 0 if this slice hidden in the animation from the given frame)
///   DWORD     Slice height
///   + If flags have bit 1
///     LONG    Center X position (relative to slice bounds)
///     LONG    Center Y position
///     DWORD   Center width
///     DWORD   Center height
///   + If flags have bit 2
///     LONG    Pivot X position (relative to the slice origin)
///     LONG    Pivot Y position (relative to the slice origin)
/// </summary>
public class SliceChunkPartition : AseChunkPartition
{
    public virtual uint Flags { get; set; }
    public virtual string Name { get; set; }
    public virtual List<SliceKey> SliceKeys { get; set; }
}

public class SliceKey
{
    public virtual uint FrameNumber { get; set; }
    public virtual int XOriginInSprite { get; set; }
    public virtual int YOriginInSprite { get; set; }
    public virtual uint Width { get; set; }
    public virtual uint Height { get; set; }
    public virtual int CenterXPos { get; set; }
    public virtual int CenterYPos { get; set; }
    public virtual uint CenterWidth { get; set; }
    public virtual uint CenterHeight { get; set; }
    public virtual int PivotXPos { get; set; }
    public virtual int PivotYPos { get; set; }
}

/// <summary>
/// Tileset Chunk (0x2023)
/// 
/// DWORD       Tileset ID
/// DWORD       Tileset flags
///               1 - Include link to external file
///               2 - Include tiles inside this file
///               4 - Tilemaps using this tileset use tile ID=0 as empty tile (this is the new format). In rare cases
///                   this bit is off, and the empty tile will be equal to 0xffffffff (used in internal versions of Aseprite)
///               8 - Aseprite will try to match modified tiles with their X flipped version automatically in
///                   Auto mode when using this tileset.
///               16 - Same for Y flips
///               32 - Same for D(iagonal) flips
/// DWORD       Number of tiles
/// WORD        Tile Width
/// WORD        Tile Height
/// SHORT       Base Index: Number to show in the screen from the tile with index 1 and so on (by default this is field
///             is 1, so the data that is displayed is equivalent to the data in memory). But it can be 0 to display
///             zero-based indexing (this field isn't used for the representation of the data in the file, it's just for
///             UI purposes).
/// BYTE[14]    Reserved
/// STRING      Name of the tileset
/// + If flag 1 is set
///   DWORD     ID of the external file. This ID is one entry of the the External Files Chunk.
///   DWORD     Tileset ID in the external file
/// + If flag 2 is set
///   DWORD     Compressed data length
///   PIXEL[]   Compressed Tileset image (see NOTE.3): (Tile Width) x (Tile Height x Number of Tiles)
/// </summary>
public class TilesetChunkPartition : AseChunkPartition
{
    public virtual uint TilesetID { get; set; }
    public virtual uint TilesetFlags { get; set; }
    public virtual uint NumberOfTiles { get; set; }
    public virtual ushort TileWidth { get; set; }
    public virtual ushort TileHeight { get; set; }
    public virtual short BaseIndex { get; set; }
    // 14 reserved bytes
    public virtual string Name { get; set; }
    public virtual uint ExternalFileID { get; set; }
    public virtual uint ExternalFileTilesetID { get; set; }
    public virtual uint CompressedDataLen { get; set; }
    public virtual byte[] CompressedTilesetImage { get; set; }
}

public class OldPaletteChunk0x4 : AseChunkPartition
{
    public List<OldPalettePacket0x4> Packets { get; set; } = new();

    public OldPaletteChunk0x4()
    {
    }

    public OldPaletteChunk0x4(BinaryReader reader)
    {
        Decode(reader);
    }

    public void Decode(BinaryReader reader)
    {
        ushort numPackets = reader.ReadUInt16();

        for (int i = 0; i < numPackets; i++)
            Packets.Add(new OldPalettePacket0x4(reader));
    }
}

public class OldPalettePacket0x4
{
    public byte NumberOfEntriesToSkipFromLastPacket { get; set; }
    public List<Color> Colors { get; set; } = new();

    public OldPalettePacket0x4()
    {
    }

    public OldPalettePacket0x4(BinaryReader reader)
    {
        Decode(reader);
    }

    public void Decode(BinaryReader reader)
    {
        NumberOfEntriesToSkipFromLastPacket = reader.ReadByte();
        ushort numColorsThisPacket = reader.ReadByte();

        for (int i = 0; i < numColorsThisPacket; i++)
        {
            byte r = reader.ReadByte();
            byte g = reader.ReadByte();
            byte b = reader.ReadByte();
            Colors.Add(Color.FromArgb(0xFF, r, g, b));
        }
    }
}

public class OldPaletteChunk0x11 : OldPaletteChunk0x4
{
    public OldPaletteChunk0x11() { }

    public OldPaletteChunk0x11(BinaryReader reader) : base(reader)
    {
    }
}