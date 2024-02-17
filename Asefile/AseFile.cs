namespace Asefile;

/// <summary>
/// Represents a deserialized aseprite file
/// </summary>
public class AseFile
{
    /// <summary>
    /// Header info that includes width, height, color depth, etc.
    /// </summary>
    public AseHeader Header { get; set; }
    
    /// <summary>
    /// The list of frames, includes layers and pixel data for each frame
    /// </summary>
    public List<AseFrame> Frames { get; set; } = new();
    
    /// <summary>
    /// The frame width
    /// </summary>
    public int Width
    {
        get => Header.WidthPixels;
        set => Header.WidthPixels = (ushort)value;
    }

    /// <summary>
    /// The frame height
    /// </summary>
    public int Height
    {
        get => Header.HeightPixels;
        set => Header.HeightPixels = (ushort)value;
    }

    /// <summary>
    /// The pixel color depth
    /// </summary>
    public int ColorDepth
    {
        get => Header.ColorDepth;
        set => Header.ColorDepth = (ushort)value;
    }

    /// <summary>
    /// Create an asefile from a given file
    /// </summary>
    /// <param name="fileName">A filename to load aseprite data from</param>
    public AseFile(string fileName)
    {
        using var file = File.OpenRead(fileName);
        using var reader = new BinaryReader(file);

        Header = new AseHeader(reader);

        for (var i = 0; i < Header.Frames; i++)
            Frames.Add(new AseFrame(reader, Header));

        if (Header.FileSize != reader.BaseStream.Position)
            throw new AseFileReadException("Inconsistent read size");
    }
}