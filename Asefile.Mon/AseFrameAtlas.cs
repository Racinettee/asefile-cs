using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Asefile.Mon;

public class AseFrameAtlas : IEnumerable<AseSprite>
{
    public Texture2D TextureData { get; init; }
    public Dictionary<string, Tag> Tags { get; init; } = new();
    private List<AseSprite> Sprites { get; init; } = new();

    public AseFrameAtlas(AseFile file, GraphicsDevice graphicsDevice)
    {
        TextureData = AseTexture2DLoader.LoadFile(file, graphicsDevice);
        var frame1Tags = file.Frames[0].Tags;
        foreach (var tag in frame1Tags)
            Tags[tag.TagName] = tag;
        for (int i = 0; i < file.Frames.Count; i++)
        {
            Sprites.Add(new AseSprite()
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
    public AseSprite this[int frameNo] => Sprites[frameNo];

    public List<AseSprite>? this[string animationName]
    {
        get
        {
            Tag? animation;
            if (!Tags.TryGetValue(animationName, out animation))
                return null;
            return Sprites.GetRange(animation.FromFrame, (animation.ToFrame - animation.FromFrame) + 1);
        }
        // set { } TBD
    }

    public AseAnimatedSprite? GetAnimation(string tagName)
    {
        Tag? animTag;
        Tags.TryGetValue(tagName, out animTag);
        
        if (animTag is null)
            return null;

        AseAnimatedSprite result = new AseAnimatedSprite(animTag, Sprites);
        return result;
    }

    public int Count => Sprites.Count;
    public IEnumerator<AseSprite> GetEnumerator() => Sprites.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}