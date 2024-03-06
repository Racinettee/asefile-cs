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
        if (tags.Length > 0)
        {
            var tagsInFile = file.Frames[0].Tags;
            
            // Ensure each tag passed in "tags" is valid
            foreach (var tag in tags)
                if (!tagsInFile.Exists(t => t.TagName == tag))
                    throw new NoSuchTag(tag);
        }

        return new SpriteFrames();
    }
}