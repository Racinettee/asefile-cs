namespace Asefile;

public class AseFile
{
    public AseHeader Header { get; set; }
    public List<AseFrame> Frames { get; set; } = new();
    
    public int Width
    {
        get => Header.WidthPixels;
        set => Header.WidthPixels = (ushort)value;
    }

    public int Height
    {
        get => Header.HeightPixels;
        set => Header.HeightPixels = (ushort)value;
    }

    public AseFile(string fileName)
    {
        using var file = File.OpenRead(fileName);
        using var reader = new BinaryReader(file);

        Header = new AseHeader(reader);
        Frames.Capacity = Header.Frames;

        for (var i = 0; i < Header.Frames; i++)
            Frames.Add(new AseFrame(reader, Header));
    }
}