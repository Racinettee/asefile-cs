using System.Collections;
using System.Collections.Generic;
using Asefile;
using Asefile.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace AsefileMG;

public struct Sprite
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
}

public class AnimatedSprite
{
    public List<Sprite> Frames { get; }
    private int CurrentTime { get; set; }
    private int CurrentFrame { get; set; }
    public void Draw(SpriteBatch spriteBatch, Vector2 pos,
        Color? color = null, Vector2? origin = null,
        Vector2? scale = null, SpriteEffects sfx = SpriteEffects.None,
        float rotation=0.0f, float layerDepth=0.0f) =>
        Frames[CurrentFrame].Draw(spriteBatch, pos, color,
            origin, scale, sfx, rotation, layerDepth);

    public void Update(GameTime gt)
    {
        Frame.Update();
    }

    public AsePlayMode Frame { get; set; }
    
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

public class FrameAtlas : IEnumerable<Sprite>
{
    public Texture2D TextureData { get; init; }
    public Dictionary<string, Tag> Tags { get; init; } = new();
    private List<Sprite> Sprites { get; init; } = new();

    public FrameAtlas(AseFile file, GraphicsDevice graphicsDevice)
    {
        TextureData = Texture2DLoader.LoadAsefile(file, graphicsDevice);
        var frame1Tags = file.Frames[0].Tags;
        foreach (var tag in frame1Tags)
            Tags[tag.TagName] = tag;
        for (int i = 0; i < file.Frames.Count; i++)
        {
            Sprites.Add(new Sprite()
            {
                Texture = TextureData,
                Duration = file.Frames[i].FrameDuration,
                Region = new Rectangle(i * file.Width, 0, file.Width, file.Height),
            });
        }
    }
    
    public void Draw(SpriteBatch spriteBatch, Vector2 pos,
        Color? color = null, Vector2? origin = null,
        Vector2? scale = null, SpriteEffects sfx = SpriteEffects.None,
        float rotation=0.0f, float layerDepth=0.0f) =>
        spriteBatch.Draw(TextureData,
            pos,
            null,
            color ?? Color.White,
            rotation,
            origin ?? Vector2.Zero,
            scale ?? Vector2.One,
            sfx, layerDepth);

    /// <summary>
    /// Access a specific frame via frame indice
    /// </summary>
    /// <param name="frameNo">The frame index to access this atlas by</param>
    public Sprite this[int frameNo] => Sprites[frameNo];

    public List<Sprite>? this[string animationName]
    {
        get
        {
            Tag? animation;
            if (!Tags.TryGetValue(animationName, out animation))
                return null;
            return Sprites.GetRange(animation.FromFrame, animation.ToFrame - animation.FromFrame);
        }
        // set { } TBD
    }

    public AnimatedSprite? GetAnimation(string tagName)
    {
        Tag? animTag;
        Tags.TryGetValue(tagName, out animTag);
        if (animTag is null)
            return null;
        
        
    }

    public int Count => Sprites.Count;
    public IEnumerator<Sprite> GetEnumerator() => Sprites.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}