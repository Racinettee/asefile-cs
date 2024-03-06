# Welcome to asefile
This library is a utility for loading aseprite image data as well as some utilities for using them in Godot and MonogGame.

- **Asefile** can be used to supply the decoded structure of an aseprite file
- **Asefile.Common** includes blending functions and some classes to step frames for animator utilities
- **AsefileMG** includes utilities to render an aseprite onto a MonoGame texture, as well as some utilities to render individual frames or some animated sprites as well.
  - An included example project demonstrates how to use those utilities **(see AsefileMGExample.cs)**
- **Asefile.Godot** includes a utility to generate a SpriteFrames from an AseFile object given a set of tags
  - An included example godot project is included that can be loaded in the editor demonstrates how to use the utility as well as generates a tres. **(see node2d.cs)**
## What is untested/unsupported?
- Grayscale and indexed colors are untested
- Encoding back into an aseprite file is unsupported as of yet
- Tiles are untested
## Future goals
- To support encoding to an aseprite file
- Test other color encodings
- Expand class libraries for monogame & godot
- Create nuget packages for godot and monogame usage respectively
- The godot SpriteFrameUtils should be improved to use the AsePlayMode class to play the animation under the hood so that ping-pong/reverse animations are generated accurately
# Example using the base library: Render in MonoGame
```csharp
private Texture2D chicaTexture;

public override void LoadContent()
{
    // Load the ase file and render it to a texture
    AseFile chicaFile = new AseFile("chica.aseprite");
    chicaTexture = GenerateTexture2DFromAseFile(chicaFile, GraphicsDevice);
}
protected override void Draw(GameTime gameTime)
{
    // ...
    // draw the texture to the window
    SpriteBatch.Draw(chicaTexture, new Vector2(100, 100), null, 
        Color.White, 0.0f, Vector2.Zero, Vector2.One * 2, SpriteEffects.None, 1f);
}
public static Texture2D GenerateTexture2DFromAseFile(AseFile aseFile, GraphicsDevice graphicsDevice)
{
    var resultTexture = new Texture2D(graphicsDevice, aseFile.Width * aseFile.Frames.Count, aseFile.Height);
    // Allocate a pixel buffer that acomodates all of the frames by multiplying the width by number of frames
    Color[] chicaBuffer = new Color[(aseFile.Width * aseFile.Frames.Count) * aseFile.Height];
    Array.Fill(chicaBuffer, Color.Transparent);
    for (int frame = 0; frame < aseFile.Frames.Count; frame++) // render each frame
    {
        int offsetX = frame * aseFile.Width;
        foreach (var cel in aseFile.Frames[frame].Cels)
        {
            // Loop through each pixel in the cel, and blit it into the buffer
            for (int celY = 0; celY < cel.HeightInPixels; celY++)
            for (int celX = 0; celX < cel.WidthInPixels; celX++)
            {
                var pixel = cel.PixelData[celX + celY * cel.WidthInPixels];
                if (pixel.A == 0)
                    continue;
                chicaBuffer[(offsetX + celX + cel.XPos + ((celY + cel.YPos) * aseFile.Width * aseFile.Frames.Count))] =
                    // asefile is System.Drawing.Color - and monogame uses Xna color
                    new Color(pixel.R, pixel.G, pixel.B, pixel.A);
            }
        }
    }
    resultTexture.SetData(chicaBuffer);
    return resultTexture;
}
```
The results will look like this:

![chica.png](chica.png)
# Example using the base library: Render in Godot 4
This example is a little bit more simple. Presuming you have a Sprite2D named Sprite2D in your tree, the code is mostly the same:
```csharp
public partial class Node2D : Godot.Node2D
{
    private Sprite2D sprite;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        AseFile aseFile = new AseFile("chica.aseprite");
        sprite = GetNode<Sprite2D>("Sprite2D");
        sprite.TextureFilter = TextureFilterEnum.Nearest;
        sprite.Texture = GenerateTexture2DFromAseFile(aseFile);
    }
    public static ImageTexture GenerateTexture2DFromAseFile(AseFile aseFile)
    {
        Image img = Image.Create(aseFile.Width * aseFile.Frames.Count, aseFile.Height, false, Image.Format.Rgba8);
        for (int frame = 0; frame < aseFile.Frames.Count; frame++) // render each frame
        {
            int offsetX = frame * aseFile.Width;
            foreach (var cel in aseFile.Frames[frame].Cels)
            {
                // Loop through each pixel in the cel, and blit it into the buffer
                for (int celY = 0; celY < cel.HeightInPixels; celY++)
                for (int celX = 0; celX < cel.WidthInPixels; celX++)
                {
                    var pixel = cel.PixelData[celX + celY * cel.WidthInPixels];
                    if (pixel.A == 0)
                        continue;
                    img.SetPixel(offsetX + celX + cel.XPos, celY + cel.YPos,
                        new Color((float)pixel.R/255, (float)pixel.G/255, (float)pixel.B/255, (float)pixel.A/255));
                }
            }
        }
        return ImageTexture.CreateFromImage(img);
    }
}
```
From that the output will look the same as the above picture, except in Godot a dark grey background is the default.
With a restructure of this code a `SpriteFrames` could be constructed for an `AnimatedSprite2D`.
# References
- The [ase-file spec](https://github.com/aseprite/aseprite/blob/main/docs/ase-file-specs.md)
- monogame-asprite [BlendFunctions.cs](https://github.com/AristurtleDev/monogame-aseprite/blob/main/source/MonoGame.Aseprite.Shared/Utilities/BlendFunctions.cs)