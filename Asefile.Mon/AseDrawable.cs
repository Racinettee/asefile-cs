using System.Collections.Generic;
using Asefile;
using Asefile.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Asefile.Mon;

public interface AseDrawable
{
    void Draw(SpriteBatch spriteBatch, Vector2 pos,
        Color? color = null, Vector2? origin = null,
        Vector2? scale = null, SpriteEffects sfx = SpriteEffects.None,
        float rotation = 0.0f, float layerDepth = 0.0f);
    
    int Width { get; }
    int Height { get; }
}

public struct AseSprite : AseDrawable
{
    /// <summary>
    /// The texture reference
    /// </summary>
    public readonly Texture2D Texture { get; init; }
    /// <summary>
    /// The number of milliseconds the frame represented by this sprite should be shown before changing
    /// </summary>
    public ushort Duration { get; init; }
    /// <summary>
    /// The region of the underlying texture to show
    /// </summary>
    public Rectangle Region { get; init; }

    public void Draw(SpriteBatch spriteBatch, Vector2 pos,
        Color? color = null, Vector2? origin = null,
        Vector2? scale = null, SpriteEffects sfx = SpriteEffects.None,
        float rotation=0.0f, float layerDepth=0.0f) =>
        spriteBatch.Draw(Texture,
            pos,
            Region,
            color ?? Color.White,
            rotation,
            origin ?? Vector2.Zero,
            scale ?? Vector2.One,
            sfx, layerDepth);

    public int Width => Region.Width;
    public int Height => Region.Height;
}

public class AseAnimatedSprite : AseDrawable
{
    public List<AseSprite> Frames { get; }
    private int CurrentTime { get; set; }

    public int CurrentFrame
    {
        get => PlayMode.Frame;
        set => PlayMode.Frame = value;
    }

    public AseAnimatedSprite(Tag animTag, List<AseSprite> allFrames)
    {
        Frames = allFrames.GetRange(animTag.FromFrame, (animTag.ToFrame - animTag.FromFrame) + 1);
        PlayMode = AsePlayMode.FromEnum(animTag.LoopDirection, (animTag.ToFrame - animTag.FromFrame) + 1); // add 1 because the frame range is inclusive
    }
    
    public void Draw(SpriteBatch spriteBatch, Vector2 pos,
        Color? color = null, Vector2? origin = null,
        Vector2? scale = null, SpriteEffects sfx = SpriteEffects.None,
        float rotation=0.0f, float layerDepth=0.0f) =>
        Frames[CurrentFrame].Draw(spriteBatch, pos, color,
            origin, scale, sfx, rotation, layerDepth);

    public int Width => Frames[CurrentFrame].Width;
    public int Height => Frames[CurrentFrame].Height;

    public void Update(GameTime gt)
    {
        if (!IsPlaying)
            return;
        
        CurrentTime += gt.ElapsedGameTime.Milliseconds;
        var targetFrame = PlayMode.Frame;

        while (CurrentTime >= Frames[targetFrame].Duration)
        {
            CurrentTime -= Frames[targetFrame].Duration;
            PlayMode.Update();  // Update the animation logic
            targetFrame = PlayMode.Frame; // In case it changed
        }
    }

    public AsePlayMode PlayMode { get; set; }
    
    public bool IsLooping { get; set; } = true; 
    public bool IsPlaying { get; set; } = true;

    public void Play() => IsPlaying = true;
    public void Pause() => IsPlaying = false;
    public void Stop()
    {
        IsPlaying = false;
        CurrentFrame = 0;
        CurrentTime = 0;
    }
}