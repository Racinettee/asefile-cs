namespace Asefile;

/*
 * DWORD       File size
   WORD        Magic number (0xA5E0)
   WORD        Frames
   WORD        Width in pixels
   WORD        Height in pixels
   WORD        Color depth (bits per pixel)
                 32 bpp = RGBA
                 16 bpp = Grayscale
                 8 bpp = Indexed
   DWORD       Flags:
                 1 = Layer opacity has valid value
   WORD        Speed (milliseconds between frame, like in FLC files)
               DEPRECATED: You should use the frame duration field
               from each frame header
   DWORD       Set be 0
   DWORD       Set be 0
   BYTE        Palette entry (index) which represent transparent color
               in all non-background layers (only for Indexed sprites).
   BYTE[3]     Ignore these bytes
   WORD        Number of colors (0 means 256 for old sprites)
   BYTE        Pixel width (pixel ratio is "pixel width/pixel height").
               If this or pixel height field is zero, pixel ratio is 1:1
   BYTE        Pixel height
   SHORT       X position of the grid
   SHORT       Y position of the grid
   WORD        Grid width (zero if there is no grid, grid size
               is 16x16 on Aseprite by default)
   WORD        Grid height (zero if there is no grid)
   BYTE[84]    For future (set to zero)
 */
public class AseHeader
{
    internal virtual uint FileSize { get; set; }
    internal virtual ushort Frames { get; set; }
    public virtual ushort WidthPixels { get; set; }
    public virtual ushort HeightPixels { get; set; }
    public virtual ushort ColorDepth { get; set; } // bpp: 32 (rgba), 16 (grayscale), 8 (indexed)
    public virtual uint Flags { get; set; }
    public virtual ushort Speed { get; set; } // DEPRECATED: use the frame duration field from each frame header
    public virtual byte PaletteEntryIndex { get; set; }
    public virtual ushort NumberOfColors { get; set; } // 0 means 256 in old sprites
    public virtual byte PixelWidth { get; set; } // if this or pixel height = 0, ratio is 1:1
    public virtual byte PixelHeight { get; set; }
    public virtual short GridXPos { get; set; } // offset of the grid
    public virtual short GridYPos { get; set; }
    public virtual ushort GridWidth { get; set; } // 0 means no grid, default is 16x16
    public virtual ushort GridHeight { get; set; }

    private const int ReservedHeaderPadding = 84;
    private const ushort AseFileMagicNumber = 0xa5e0;

    public AseHeader(BinaryReader reader)
    {
        FileSize = reader.ReadUInt32();
        var magicNumber = reader.ReadUInt16();
        if (magicNumber != AseFileMagicNumber)
            throw new AseFileReadException("invalid ase file, magic number not found");
        Frames = reader.ReadUInt16();
        WidthPixels = reader.ReadUInt16();
        HeightPixels = reader.ReadUInt16();
        ColorDepth = reader.ReadUInt16();
        Flags = reader.ReadUInt32();
        Speed = reader.ReadUInt16();
        var _zero1 = reader.ReadUInt32();
        var _zero2 = reader.ReadUInt32();
        PaletteEntryIndex = reader.ReadByte();
        reader.Read(new byte[3]);
        NumberOfColors = reader.ReadUInt16();
        PixelWidth = reader.ReadByte();
        PixelHeight = reader.ReadByte();
        GridXPos = reader.ReadInt16();
        GridYPos = reader.ReadInt16();
        GridWidth = reader.ReadUInt16();
        GridHeight = reader.ReadUInt16();
        reader.Read(new byte[ReservedHeaderPadding]); // 84 bytes for future use
    }

    void Encode(BinaryWriter writer)
    {
        writer.Write(FileSize);
        writer.Write(AseFileMagicNumber);
        writer.Write(Frames);
        writer.Write(WidthPixels);
        writer.Write(HeightPixels);
        writer.Write(ColorDepth);
        writer.Write(Flags);
        writer.Write(Speed);
        writer.Write(0u); // zero1
        writer.Write(0u); // zero2
        writer.Write(PaletteEntryIndex);
        writer.Write(new byte[3]);
        writer.Write(NumberOfColors);
        writer.Write(PixelWidth);
        writer.Write(PixelHeight);
        writer.Write(GridXPos);
        writer.Write(GridYPos);
        writer.Write(GridWidth);
        writer.Write(GridHeight);
        writer.Write(new byte[ReservedHeaderPadding]);
    }
}

/*
 * DWORD       Bytes in this frame
   WORD        Magic number (always 0xF1FA)
   WORD        Old field which specifies the number of "chunks"
               in this frame. If this value is 0xFFFF, we might
               have more chunks to read in this frame
               (so we have to use the new field)
   WORD        Frame duration (in milliseconds)
   BYTE[2]     For future (set to zero)
   DWORD       New field which specifies the number of "chunks"
               in this frame (if this is 0, use the old field)
 */
public class AseFrame
{
    public ushort                  FrameDuration { get; set; }
    public List<PaletteChunkPartition>      Palettes { get; set; } = new();
    public List<LayerChunkPartition>        Layers { get; set; } = new();
    public List<CelChunkPartition>          Cels { get; set; } = new();
    public List<ColorProfileChunkPartition> ColorProfiles { get; set; } = new();
    public List<Tag>               Tags { get; set; } = new();

    private AseHeader header;

    public AseFrame(BinaryReader reader, AseHeader header)
    {
        this.header = header;
        Decode(reader);
    }

    public void Decode(BinaryReader reader)
    {
        var bytesThisFrame = reader.ReadUInt32() - 4; // exludes this dword
        var streamOffsetBegin = reader.BaseStream.Position;
        
        var magicNumber = reader.ReadUInt16();
        if (magicNumber != 0xF1FA)
            throw new AseFileReadException("invalid frame header data read");
        var oldChunksNum = reader.ReadUInt16();
        FrameDuration = reader.ReadUInt16();
        // reserved for future
        reader.Read(new byte[2]);

        var useChunksNum = reader.ReadUInt32();

        if (useChunksNum == 0)
            useChunksNum = oldChunksNum;

        AseChunkPartition? lastReadChunk = null;
        
        for (int i = 0; i < useChunksNum; i++)
        {
            var chunkSize = reader.ReadUInt32();
            var chunkType = reader.ReadUInt16();
            
            switch ((AseChunkType)chunkType)
            {
                case AseChunkType.OldPalette1:
                    lastReadChunk = new OldPaletteChunk0x4(reader);
                    break;
                case AseChunkType.OldPalette2:
                    lastReadChunk = new OldPaletteChunk0x11(reader);
                    break;
                case AseChunkType.Layer:
                    lastReadChunk = new LayerChunkPartition(reader);
                    Layers.Add((LayerChunkPartition)lastReadChunk);
                    break;
                case AseChunkType.Cel:
                    lastReadChunk = new CelChunkPartition(reader, header, (int)chunkSize);
                    Cels.Add((CelChunkPartition)lastReadChunk);
                    break;
                case AseChunkType.CelExtra:
                    lastReadChunk = new CelExtraChunkPartition(reader);
                    // Safe bet that theres a last if we got here
                    Cels.Last().Extra = (CelExtraChunkPartition)lastReadChunk; 
                    break;
                case AseChunkType.ColorProfile:
                    lastReadChunk = new ColorProfileChunkPartition(reader);
                    ColorProfiles.Add((ColorProfileChunkPartition)lastReadChunk);
                    break;
                case AseChunkType.Tags:
                    lastReadChunk = new TagChunkPartition(reader);
                    var tags = ((TagChunkPartition)lastReadChunk).Tags;
                    
                    foreach (var tag in tags)
                    {
                        var _userDataChunkSize = reader.ReadUInt32();
                        var _userDataChunkType = reader.ReadUInt16();
                        if ((AseChunkType)_userDataChunkType != AseChunkType.UserData)
                        {
                            reader.BaseStream.Position -= 6; // U32 + U16 = 6 bytes
                            break;
                        }

                        tag.UserData = new UserDataChunkPartition(reader);
                        i++; // these count towards the total chunks!
                    }

                    Tags.AddRange(((TagChunkPartition)lastReadChunk).Tags);
                    break;
                case AseChunkType.Palette:
                    lastReadChunk = new PaletteChunkPartition(reader);
                    Palettes.Add((PaletteChunkPartition)lastReadChunk);
                    break;
                case AseChunkType.UserData:
                    lastReadChunk.UserData = new UserDataChunkPartition(reader);
                    break;
                default:
                    throw new AseFileReadException("unimplemented chunk type!");
            }
        }

        var streamOffsetEnd = reader.BaseStream.Position;

        if ((streamOffsetEnd - streamOffsetBegin) != bytesThisFrame)
            throw new AseFileReadException("incorrect frame size read");
    }
}