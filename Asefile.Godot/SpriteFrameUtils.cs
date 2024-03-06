using Asefile.Common;
using Godot;

namespace Asefile.Godot;

public static class SpriteFrameUtils
{
    public class NoSuchTag : Exception
    {
        public NoSuchTag(string tagName) : base($"No such tag {tagName}")
        {
        }
    }

    public static SpriteFrames LoadFrames(AseFile file, params string[] tags)
    {
        // Validation step: check if all the requested tags match whats in the file
        if (tags.Length > 0)
        {
            var tagsInFile = file.Frames[0].Tags;
            
            // Ensure each tag passed in "tags" is valid
            foreach (var tag in tags)
                if (!tagsInFile.Exists(t => t.TagName == tag))
                    throw new NoSuchTag(tag);
        }
        
        // The result, which will be a spriteframes resource with all the animations of interest
        SpriteFrames sf = new SpriteFrames();

        foreach (var tag in tags)
        {
            // Add the new animation, by the name of tag
            sf.AddAnimation(new StringName(tag));
            
            // Get the tag data, it specifies the frames in a given animation
            var tagData = file.Frames[0].Tags.Single(t => t.TagName == tag);
            
            for (int frame = tagData.FromFrame; frame <= tagData.ToFrame; frame++)
            {
                // Render each frame and add its data as a frame into the animation "tag"
                Image img = Image.Create(file.Width, file.Height, false, Image.Format.Rgba8);

                foreach (var cel in file.Frames[frame].Cels)
                {
                    var layer = file.Frames[0].Layers[cel.LayerIndex];
                    var blendFunc = AseBlender.GetBlender(layer.BlendMode);
                    
                    // Loop through each pixel in the cel, and blit it into the buffer
                    for (int celY = 0; celY < cel.HeightInPixels; celY++)
                    for (int celX = 0; celX < cel.WidthInPixels; celX++)
                    {
                        // We blend this over top of backdrop pix shortly:
                        var sourcePix = cel.PixelData[celX + celY * cel.WidthInPixels];
                        if (sourcePix.A == 0)
                            continue;
                        
                        // We need to blend this cel to replicate whats shown in actual aseprite
                        // First: we have to get the existing color thats already in the image:
                        var backdropPix = img.GetPixel(celX + cel.XPos, celY + cel.YPos);
                        var backdropPixSys = System.Drawing.Color
                            .FromArgb(backdropPix.A8, backdropPix.R8, backdropPix.G8, backdropPix.B8);
    
                        // Do the blending, it returns us the blended color value, and then we apply it to the image
                        var blendPix = blendFunc(sourcePix, backdropPixSys, layer.Opacity);
                        
                        img.SetPixel(celX + cel.XPos, celY + cel.YPos,
                            new Color((float)blendPix.R/255,
                            (float)blendPix.G/255,
                            (float)blendPix.B/255,
                            (float)blendPix.A/255));
                    }
                }

                sf.AddFrame(new StringName(tag), ImageTexture.CreateFromImage(img),
                    file.Frames[frame].FrameDuration / 1000.0f);
            }
        }
        return sf;
    }
}