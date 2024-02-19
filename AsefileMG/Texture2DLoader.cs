using System;
using Asefile;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AsefileMG;

public static class Texture2DLoader
{
    public static Texture2D LoadAsefile(AseFile file, GraphicsDevice graphicsDevice)
    {
        var resultTexture = new Texture2D(graphicsDevice, file.Width * file.Frames.Count, file.Height);
        // Allocate a pixel buffer that acomodates all of the frames by multiplying the width by number of frames
        Color[] chicaBuffer = new Color[(file.Width * file.Frames.Count) * file.Height];
        Array.Fill(chicaBuffer, Color.Transparent);
        for (int frame = 0; frame < file.Frames.Count; frame++) // render each frame
        {
            int offsetX = frame * file.Width;
            foreach (var cel in file.Frames[frame].Cels)
            {
                // Loop through each pixel in the cel, and blit it into the buffer
                for (int celY = 0; celY < cel.HeightInPixels; celY++)
                for (int celX = 0; celX < cel.WidthInPixels; celX++)
                {
                    var pixel = cel.PixelData[celX + celY * cel.WidthInPixels];
                    if (pixel.A == 0)
                        continue;
                    chicaBuffer[(offsetX + celX + cel.XPos + ((celY + cel.YPos) * file.Width * file.Frames.Count))] =
                        // asefile is System.Drawing.Color - and monogame uses Xna color
                        new Color((byte)pixel.R, (byte)pixel.G, (byte)pixel.B, (byte)pixel.A);
                }
            }
        }
        resultTexture.SetData(chicaBuffer);
        return resultTexture;
    }
}